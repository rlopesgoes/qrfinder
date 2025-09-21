using FluentValidation;

namespace Application.Videos.VideoUploader;

public class VideoUploaderValidator : AbstractValidator<VideoUploaderRequest>
{
    public VideoUploaderValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId é obrigatório")
            .Length(32).WithMessage("VideoId deve ter 32 caracteres");
        
        RuleFor(x => x.TotalBytes)
            .GreaterThan(0).WithMessage("TotalBytes deve ser maior que zero")
            .LessThanOrEqualTo(104857600).WithMessage("Arquivo não pode exceder 100MB");
        
        RuleFor(x => x.Source)
            .NotNull().WithMessage("Stream é obrigatório");
    }
}