﻿using BulkyStore_DataAccess.Data;
using BulkyStore_DataAccess.Repository.IRepository;

namespace BulkyStore_DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private AppDbContext _db;
        public ICategoryRepository Category { get; private set; }
        //public ICompanyRepository Company { get; private set; }
        public IProductRepository Product { get; private set; }
        //public IShoppingCartRepository ShoppingCart { get; private set; }
        //public IApplicationUserRepository ApplicationUser { get; private set; }
        //public IOrderHeaderRepository OrderHeader { get; private set; }
        //public IOrderDetailRepository OrderDetail { get; private set; }
        //public IProductImageRepository ProductImage { get; private set; }
        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            //ProductImage = new ProductImageRepository(_db);
            //ApplicationUser = new ApplicationUserRepository(_db);
            //ShoppingCart = new ShoppingCartRepository(_db);
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            //Company = new CompanyRepository(_db);
            //OrderHeader = new OrderHeaderRepository(_db);
            //OrderDetail = new OrderDetailRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
