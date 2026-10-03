using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using unilib.Models;

namespace unilib.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext()
    {
    }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<UserBook> UserBooks => Set<UserBook>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing in appsettings.json.");

        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("role");
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(r => r.Name).IsUnique();
            e.HasData(
                new Role { Id = 1, Name = RoleNames.Admin },
                new Role { Id = 2, Name = RoleNames.Librarian },
                new Role { Id = 3, Name = RoleNames.Student });
        });

        modelBuilder.Entity<Faculty>(e =>
        {
            e.ToTable("faculty");
            e.Property(f => f.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(f => f.Name).IsUnique();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("category");
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Library>(e =>
        {
            e.ToTable("library");
            e.Property(l => l.Name).HasMaxLength(150).IsRequired();
            e.Property(l => l.Address).HasMaxLength(255);
            e.Property(l => l.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("user");
            e.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(50).IsRequired();
            e.Property(u => u.StudentId).HasMaxLength(20);
            e.Property(u => u.Email).HasMaxLength(100).IsRequired();
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.CreatedAt).IsRequired();
            e.HasIndex(u => u.StudentId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();

            e.HasOne(u => u.Role).WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(u => u.Faculty).WithMany(f => f.Users)
                .HasForeignKey(u => u.FacultyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(u => u.Library).WithMany(l => l.Users)
                .HasForeignKey(u => u.LibraryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Author>(e =>
        {
            e.ToTable("author");
            e.Property(a => a.FirstName).HasMaxLength(50).IsRequired();
            e.Property(a => a.LastName).HasMaxLength(50).IsRequired();
            e.Property(a => a.Biography).HasColumnType("text");

            e.HasOne(a => a.User).WithMany(u => u.Authors)
                .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Book>(e =>
        {
            e.ToTable("book");
            e.HasKey(b => b.Isbn);
            e.Property(b => b.Isbn).HasMaxLength(20);
            e.Property(b => b.Title).HasMaxLength(200).IsRequired();
            e.Property(b => b.Language).HasMaxLength(50);
            e.Property(b => b.Description).HasColumnType("text");

            e.HasOne(b => b.Faculty).WithMany(f => f.Books)
                .HasForeignKey(b => b.FacultyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Library).WithMany(l => l.Books)
                .HasForeignKey(b => b.LibraryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Category).WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookAuthor>(e =>
        {
            e.ToTable("book_author");
            e.HasKey(ba => new { ba.BookId, ba.AuthorId });
            e.Property(ba => ba.BookId).HasMaxLength(20);

            e.HasOne(ba => ba.Book).WithMany(b => b.BookAuthors)
                .HasForeignKey(ba => ba.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ba => ba.Author).WithMany(a => a.BookAuthors)
                .HasForeignKey(ba => ba.AuthorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserBook>(e =>
        {
            e.ToTable("user_book");
            e.Property(ub => ub.BookIsbn).HasMaxLength(20).IsRequired();
            e.Property(ub => ub.Status).HasMaxLength(30).IsRequired();

            e.HasOne(ub => ub.Book).WithMany(b => b.Loans)
                .HasForeignKey(ub => ub.BookIsbn).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ub => ub.User).WithMany(u => u.Loans)
                .HasForeignKey(ub => ub.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.ToTable("reservation");
            e.Property(r => r.BookId).HasMaxLength(20).IsRequired();
            e.Property(r => r.Status).HasMaxLength(30).IsRequired();

            e.HasOne(r => r.Book).WithMany(b => b.Reservations)
                .HasForeignKey(r => r.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Favorite>(e =>
        {
            e.ToTable("favorite");
            e.HasKey(f => new { f.UserId, f.BookId });
            e.Property(f => f.BookId).HasMaxLength(20);

            e.HasOne(f => f.User).WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Book).WithMany(b => b.Favorites)
                .HasForeignKey(f => f.BookId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
