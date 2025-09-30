#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

class LoadTest {
    constructor() {
        this.baseUrl = 'http://localhost/app';
        this.totalVideos = 10;
        this.durationMinutes = 1;
        this.testVideoPath = path.join(__dirname, '..', 'tests', 'sample-videos', 'teste.mp4');
        this.results = {
            totalVideos: 0,
            successful: 0,
            failed: 0,
            uploadTimes: [],
            errors: [],
            startTime: null,
            endTime: null
        };
    }

    async sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    formatDuration(ms) {
        const seconds = Math.floor(ms / 1000);
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = seconds % 60;
        return `${minutes}m ${remainingSeconds}s`;
    }

    formatFileSize(bytes) {
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        if (bytes === 0) return '0 Bytes';
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
    }

    async generateUploadLink() {
        const response = await fetch(`${this.baseUrl}/video/upload-link/generate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });

        if (!response.ok) {
            throw new Error(`Failed to generate upload link: ${response.status}`);
        }

        return await response.json();
    }

    async uploadVideo(uploadUrl, videoBuffer) {
        const fixedUrl = uploadUrl.replace('http://localhost:10000', 'http://localhost/storage');
        
        const response = await fetch(fixedUrl, {
            method: 'PUT',
            headers: {
                'x-ms-blob-type': 'BlockBlob',
                'Content-Type': 'video/mp4'
            },
            body: videoBuffer
        });

        if (!response.ok) {
            throw new Error(`Upload failed: ${response.status}`);
        }
    }

    async startAnalysis(videoId) {
        const response = await fetch(`${this.baseUrl}/video/${videoId}/analyze`, {
            method: 'PATCH'
        });

        if (!response.ok) {
            throw new Error(`Analysis start failed: ${response.status}`);
        }

        return await response.json();
    }

    async processVideo(videoId, videoBuffer) {
        const startTime = Date.now();
        
        try {
            // 1. Generate upload link
            const linkData = await this.generateUploadLink();
            
            // 2. Upload video
            await this.uploadVideo(linkData.url, videoBuffer);
            
            // 3. Start analysis
            await this.startAnalysis(linkData.videoId);
            
            const endTime = Date.now();
            const uploadTime = endTime - startTime;
            
            this.results.successful++;
            this.results.uploadTimes.push(uploadTime);
            
            console.log(`✅ Video ${videoId} uploaded and queued in ${uploadTime}ms`);
            
        } catch (error) {
            this.results.failed++;
            this.results.errors.push({ videoId, error: error.message });
            console.log(`❌ Video ${videoId} failed: ${error.message}`);
        }
    }

    async runLoadTest() {
        console.log(`🚀 Starting load test: ${this.totalVideos} videos in ${this.durationMinutes} minutes`);
        console.log(`📁 Using test video: ${this.testVideoPath}`);
        
        // Check if test video exists
        if (!fs.existsSync(this.testVideoPath)) {
            console.error(`❌ Test video not found: ${this.testVideoPath}`);
            process.exit(1);
        }

        // Load video file once
        const videoBuffer = fs.readFileSync(this.testVideoPath);
        const videoSize = videoBuffer.length;
        console.log(`📊 Video size: ${this.formatFileSize(videoSize)}`);
        
        console.log(`⏱️  Mode: Concurrent uploads (true stress test)`);
        console.log(`📈 Target: All ${this.totalVideos} videos simultaneously`);
        console.log(`🎯 Total bandwidth: ~${this.formatFileSize(videoSize * this.totalVideos)}`);
        console.log('');

        this.results.startTime = Date.now();
        
        // Start uploads concurrently (test real concurrency)
        const promises = [];
        for (let i = 1; i <= this.totalVideos; i++) {
            promises.push(this.processVideo(i, videoBuffer));
            
            // Progress indicator
            if (i % 5 === 0) {
                console.log(`📋 Scheduled ${i}/${this.totalVideos} uploads...`);
            }
        }

        // Wait for all uploads to complete
        console.log(`⏳ Waiting for all ${this.totalVideos} uploads to complete...`);
        await Promise.all(promises);
        
        this.results.endTime = Date.now();
        this.generateReport();
    }

    generateReport() {
        const totalDuration = this.results.endTime - this.results.startTime;
        const successRate = (this.results.successful / this.totalVideos * 100).toFixed(1);
        
        const avgUploadTime = this.results.uploadTimes.length > 0 
            ? Math.round(this.results.uploadTimes.reduce((a, b) => a + b, 0) / this.results.uploadTimes.length)
            : 0;
            
        const minUploadTime = this.results.uploadTimes.length > 0 
            ? Math.min(...this.results.uploadTimes) 
            : 0;
            
        const maxUploadTime = this.results.uploadTimes.length > 0 
            ? Math.max(...this.results.uploadTimes) 
            : 0;

        const videosPerMinute = Math.round(this.results.successful / (totalDuration / 60000));

        console.log('\n🎯 LOAD TEST RESULTS');
        console.log('='.repeat(50));
        console.log(`📊 Total Videos: ${this.totalVideos}`);
        console.log(`✅ Successful: ${this.results.successful}`);
        console.log(`❌ Failed: ${this.results.failed}`);
        console.log(`📈 Success Rate: ${successRate}%`);
        console.log(`⏱️  Total Duration: ${this.formatDuration(totalDuration)}`);
        console.log(`🚀 Actual Rate: ${videosPerMinute} videos/min`);
        console.log('');
        console.log('📋 Upload Times:');
        console.log(`   Average: ${avgUploadTime}ms`);
        console.log(`   Min: ${minUploadTime}ms`);
        console.log(`   Max: ${maxUploadTime}ms`);
        
        if (this.results.errors.length > 0) {
            console.log('\n❌ ERRORS:');
            this.results.errors.forEach(({ videoId, error }, index) => {
                if (index < 10) { // Show first 10 errors
                    console.log(`   Video ${videoId}: ${error}`);
                }
            });
            if (this.results.errors.length > 10) {
                console.log(`   ... and ${this.results.errors.length - 10} more errors`);
            }
        }

        // Performance Analysis
        console.log('\n📊 PERFORMANCE ANALYSIS:');
        console.log('='.repeat(50));
        if (successRate >= 95) {
            console.log('🟢 EXCELLENT: System handled load very well');
        } else if (successRate >= 90) {
            console.log('🟡 GOOD: System performed well with minor issues');
        } else if (successRate >= 80) {
            console.log('🟠 FAIR: System showed some stress under load');
        } else {
            console.log('🔴 POOR: System struggled significantly under load');
        }

        if (avgUploadTime < 1000) {
            console.log('🟢 Response times are excellent (< 1s avg)');
        } else if (avgUploadTime < 3000) {
            console.log('🟡 Response times are acceptable (< 3s avg)');
        } else {
            console.log('🔴 Response times are concerning (> 3s avg)');
        }

        console.log('\n💡 RECOMMENDATIONS:');
        if (this.results.failed > 0) {
            console.log('- Review error logs and increase timeout values');
            console.log('- Consider scaling up infrastructure resources');
        }
        if (avgUploadTime > 2000) {
            console.log('- Optimize upload process and API response times');
            console.log('- Check network bandwidth and storage performance');
        }
        if (successRate < 95) {
            console.log('- Implement retry mechanisms for failed uploads');
            console.log('- Add circuit breaker patterns for resilience');
        }

        // Save results to file
        const reportFile = path.join(__dirname, 'load-test-results.json');
        fs.writeFileSync(reportFile, JSON.stringify(this.results, null, 2));
        console.log(`\n📄 Detailed results saved to: ${reportFile}`);
    }
}

// CLI execution
async function main() {
    const args = process.argv.slice(2);
    const loadTest = new LoadTest();

    // Parse command line arguments
    args.forEach(arg => {
        if (arg.startsWith('--videos=')) {
            loadTest.totalVideos = parseInt(arg.split('=')[1]);
        } else if (arg.startsWith('--duration=')) {
            loadTest.durationMinutes = parseInt(arg.split('=')[1]);
        } else if (arg.startsWith('--url=')) {
            loadTest.baseUrl = arg.split('=')[1];
        }
    });

    try {
        await loadTest.runLoadTest();
    } catch (error) {
        console.error(`💥 Load test failed: ${error.message}`);
        process.exit(1);
    }
}

if (require.main === module) {
    main();
}