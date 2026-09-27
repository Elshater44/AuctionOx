using AuctionOx.Models;
using System;
using System.Threading.Tasks;

namespace AuctionOx.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Category> Categories { get; }
        IRepository<AuctionItem> Auctions { get; }
        IRepository<Bid> Bids { get; }

        Task<int> CompleteAsync();
    }
}
