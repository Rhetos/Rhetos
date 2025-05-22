SELECT
    b.ID,
    Description = b.Title + ISNULL(', ' + p.Name, '')
FROM
    Bookstore.Book b
    LEFT JOIN Bookstore.Person p ON p.ID = b.AuthorID
