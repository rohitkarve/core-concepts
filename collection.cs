using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Differences between IQueryable, List, IEnumerable in C#
/// With MySQL/Database context and memory considerations
/// </summary>

// =============================================================================
// 1. IQueryable<T> - DEFERRED EXECUTION, QUERY STAYS ON DATABASE
// =============================================================================
// - Query is NOT executed until you enumerate (foreach, ToList, Count, etc.)
// - SQL is built and sent to database only when needed
// - Best for: Large datasets, filtering at database level

public class IQueryableExample
{
    public void Example(MyDbContext context)
    {
        // NO database call yet - just building expression tree
        IQueryable<User> query = context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name);

        // STILL no database call
        query = query.Where(u => u.Age > 18);

        // NOW the SQL is executed (only fetches matching records)
        List<User> results = query.ToList();
        
        // Generated SQL: SELECT * FROM Users WHERE IsActive = 1 AND Age > 18 ORDER BY Name
    }
}

// =============================================================================
// 2. IEnumerable<T> - DEFERRED EXECUTION, BUT IN MEMORY
// =============================================================================
// - Once data leaves database, filtering happens in C# memory
// - Fetches ALL data first, then filters locally

public class IEnumerableExample
{
    public void Example(MyDbContext context)
    {
        // This fetches ALL users into memory first!
        IEnumerable<User> users = context.Users.AsEnumerable();

        // Filtering happens in C# memory, NOT in database
        var filtered = users.Where(u => u.Age > 18); // BAD for large datasets!
        
        // All 1 million users loaded, then filtered in memory
    }
}

// =============================================================================
// 3. List<T> - IMMEDIATE EXECUTION, ALL IN MEMORY
// =============================================================================
// - Data is loaded immediately into memory
// - Fast for repeated access, but uses more memory

public class ListExample
{
    public void Example(MyDbContext context)
    {
        // Database query executes IMMEDIATELY
        List<User> users = context.Users.Where(u => u.IsActive).ToList();

        // Data is now in memory - no more database calls
        var count = users.Count;        // Memory operation
        var first = users.FirstOrDefault(); // Memory operation
        
        // Good when you need to iterate multiple times
        foreach (var user in users) { /* ... */ }
        foreach (var user in users) { /* ... */ } // No additional DB call
    }
}

// =============================================================================
// REAL-LIFE BEST PRACTICES
// =============================================================================

public class BestPractices
{
    private readonly MyDbContext _context;

    // ✅ GOOD: Keep as IQueryable for flexible filtering
    public IQueryable<User> GetUsersQuery()
    {
        return _context.Users.Where(u => u.IsActive);
    }

    // ✅ GOOD: Use IQueryable for pagination (only fetches needed records)
    public async Task<List<User>> GetUsersPaged(int page, int pageSize)
    {
        return await _context.Users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(); // SQL: SELECT * FROM Users LIMIT 10 OFFSET 0
    }

    // ❌ BAD: Loading all data then filtering in memory
    public List<User> BadExample()
    {
        var allUsers = _context.Users.ToList(); // Loads ALL users!
        return allUsers.Where(u => u.Age > 18).ToList(); // Filters in memory
    }

    // ✅ GOOD: Filter at database level
    public List<User> GoodExample()
    {
        return _context.Users
            .Where(u => u.Age > 18) // SQL WHERE clause
            .ToList();
    }

    // ✅ GOOD: Use List when you need multiple iterations
    public void MultipleIterations()
    {
        var users = _context.Users.Where(u => u.IsActive).ToList();
        
        var totalCount = users.Count;
        var averageAge = users.Average(u => u.Age);
        var names = users.Select(u => u.Name).ToList();
        // Only ONE database call for all operations
    }

    // ✅ GOOD: Select only needed columns
    public List<UserDto> ProjectionExample()
    {
        return _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDto 
            { 
                Id = u.Id, 
                Name = u.Name 
            }) // Only fetches Id and Name columns
            .ToList();
    }

    // ✅ GOOD: Use AsNoTracking for read-only queries
    public List<User> ReadOnlyExample()
    {
        return _context.Users
            .AsNoTracking() // Better performance for read-only
            .Where(u => u.IsActive)
            .ToList();
    }
}

// =============================================================================
// MEMORY COMPARISON TABLE
// =============================================================================
/*
| Type          | When Data Loads      | Where Filtering Happens | Best Use Case                    |
|---------------|----------------------|-------------------------|----------------------------------|
| IQueryable<T> | On enumeration       | Database (SQL)          | Large datasets, dynamic queries  |
| IEnumerable<T>| On enumeration       | Memory (C#)             | In-memory collections            |
| List<T>       | Immediately          | Memory (C#)             | Multiple iterations, small data  |
*/

// =============================================================================
// SUPPORTING CLASSES
// =============================================================================

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class MyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
}