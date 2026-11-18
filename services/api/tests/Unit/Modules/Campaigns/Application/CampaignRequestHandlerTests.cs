using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using Api.SharedKernel;
using FluentAssertions;
using MediatR;

namespace Unit.Modules.Campaigns.Application;

public sealed class CampaignRequestHandlerTests
{
    [Fact]
    public void CampaignUseCasesAreMediatRRequestsHandledByTheirFeatureHandlers()
    {
        typeof(CreateCampaignCommand).Should().Implement<IRequest<Result<CampaignDto>>>();
        typeof(CreateCampaignHandler).Should().Implement<IRequestHandler<CreateCampaignCommand, Result<CampaignDto>>>();
        typeof(ListCampaignsQuery).Should().Implement<IRequest<Result<IReadOnlyList<CampaignDto>>>>();
        typeof(ListCampaignsHandler).Should().Implement<IRequestHandler<ListCampaignsQuery, Result<IReadOnlyList<CampaignDto>>>>();
        typeof(GetCampaignQuery).Should().Implement<IRequest<Result<CampaignDto>>>();
        typeof(GetCampaignHandler).Should().Implement<IRequestHandler<GetCampaignQuery, Result<CampaignDto>>>();
    }
}
