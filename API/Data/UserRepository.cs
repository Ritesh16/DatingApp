using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUserNameAsync(string userName)
        {
            return await context.Users.Include(p => p.Photos)
                            .SingleOrDefaultAsync(x => x.UserName == userName);
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await context.Users.Include(p => p.Photos).ToListAsync();
        }

        public async Task<MemberDto> GetMemberAsync(string userName)
        {
            return await context.Users
                         .Where(x => x.UserName == userName.ToLower())
                         .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
                         .SingleOrDefaultAsync();
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var query = context.Users.AsQueryable();

            query = query.Where(x => x.UserName != userParams.CurrentUserName);
            query = query.Where(x => x.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(x => x.Created),
                _ => query.OrderByDescending(x => x.LastActive)
            };

            return await PagedList<MemberDto>.CreateAsync(query.ProjectTo<MemberDto>(
                mapper.ConfigurationProvider).AsNoTracking(), userParams.PageNumber, userParams.PageSize);
        }
        public void Update(AppUser user)
        {
            context.Entry(user).State = EntityState.Modified;
        }

        public async Task<string> GetUserGender(string userName)
        {
            return await context.Users
                        .Where(x => x.UserName == userName)
                        .Select(x => x.Gender).FirstOrDefaultAsync();
        }
    }
}