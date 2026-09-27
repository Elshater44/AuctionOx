using AuctionOx.Data;
using AuctionOx.Models;
using AuctionOx.Repositories.Interfaces;
using System.Threading.Tasks;

namespace AuctionOx.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ApplicationDbContext Context => _context;

        public IRepository<Category> Categories { get; private set; }
        public IRepository<AuctionItem> Auctions { get; private set; }
        public IRepository<Bid> Bids { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Categories = new Repository<Category>(_context);
            Auctions = new Repository<AuctionItem>(_context);
            Bids = new Repository<Bid>(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
