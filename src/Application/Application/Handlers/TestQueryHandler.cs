using Application.Queries;
using MediatR;

namespace Application.Handlers;

public class TestQueryHandler : IRequestHandler<TestQuery, TestQueryResponse>
{
    public Task<TestQueryResponse> Handle(TestQuery request, CancellationToken cancellationToken)
    {
        var response = new TestQueryResponse(
            Result: $"Processed message: {request.Message}",
            Timestamp: DateTime.UtcNow
        );

        return Task.FromResult(response);
    }
}