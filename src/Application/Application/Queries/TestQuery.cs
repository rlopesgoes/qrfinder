using MediatR;

namespace Application.Queries;

public record TestQuery(string Message) : IRequest<TestQueryResponse>;

public record TestQueryResponse(string Result, DateTime Timestamp);