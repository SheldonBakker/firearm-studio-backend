using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Dashboard.GetDashboardStats;

public sealed record GetDashboardStatsQuery : IQuery<ErrorOr<DashboardStatsResponse>>;
