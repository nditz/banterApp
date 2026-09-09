using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Receipts;

public static class ReceiptPrivacy
{
    /// <summary>
    /// Phase 4 receipts stay off the public timeline. Studio/history are owner-scoped.
    /// </summary>
    public static bool IsEligibleForPublicTimeline(PredictionReceipt receipt) =>
        receipt.IsPublic;
}
