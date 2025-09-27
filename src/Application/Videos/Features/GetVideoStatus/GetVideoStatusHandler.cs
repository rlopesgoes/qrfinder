// using Application.Videos.Ports;
// using MediatR;
//
// namespace Application.Videos.Features.GetVideoStatus;
//
// public class GetVideoStatusHandler(IProcessStatusRepository repository) 
//     : IRequestHandler<GetVideoStatusRequest, GetVideoStatusResponse?>
// {
//     public async Task<GetVideoStatusResponse?> Handle(GetVideoStatusRequest request, CancellationToken cancellationToken)
//     {
//         var uploadStatus = await repository.GetAsync(request.VideoId, cancellationToken);
//         
//         if (uploadStatus == null)
//             return null;
//
//         return new GetVideoStatusResponse(
//             uploadStatus.VideoId,
//             uploadStatus.Stage.ToString());
//     }
// }