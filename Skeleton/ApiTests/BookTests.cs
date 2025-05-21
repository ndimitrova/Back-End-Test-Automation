﻿using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;
using System.Xml.Linq;

namespace ApiTests
{
    [TestFixture]
    public class BookTests : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
        }

        [Test]
        public void Test_GetAllBooks()
        {
            var request = new RestRequest("book", Method.Get);

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");

                var books = JArray.Parse(response.Content);

                Assert.That(books.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response content to be a JSON array");
                Assert.That(books.Count, Is.GreaterThan(0), "Expected at least one book in the response");

                foreach (var book in books)
                {
                    Assert.That(book["title"]?.ToString(), Is.Not.Null.And.Not.Empty,
                        "Book title should not be null or empty");
                    Assert.That(book["author"]?.ToString(), Is.Not.Null.And.Not.Empty,
                        "Book author should not be null or empty");
                    Assert.That(book["description"]?.ToString(), Is.Not.Null.And.Not.Empty,
                        "Book description should not be null or empty");
                    Assert.That(book["price"]?.ToString(), Is.Not.Null.And.Not.Empty,
                        "Book price should not be null or empty");
                    Assert.That(book["pages"]?.ToString(), Is.Not.Null.And.Not.Empty,
                         "Book pages should not be null or empty");
                    Assert.That(book["category"]?.ToString(), Is.Not.Null.And.Not.Empty,
                        "Book category should not be null or empty");
                }
            });

        }

        [Test]
        public void Test_GetBookByTitle()
        {
            var request = new RestRequest("book", Method.Get);

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");

                var books = JArray.Parse(response.Content);
                var book = books.FirstOrDefault(b => b["title"]?.ToString() == "The Great Gatsby");

                Assert.That(book["author"]?.ToString(), Is.EqualTo("F. Scott Fitzgerald"),
                    "Book author should be different");
                
            });

        }

        [Test]
        public void Test_AddBook()
        {
            var getCategoriesRequest = new RestRequest($"category", Method.Get);
            var getCategoriesResponse = client.Execute(getCategoriesRequest);

            var categories = JArray.Parse(getCategoriesResponse.Content);

            var category = categories.First();
            var categoryId = category["_id"]?.ToString();

            var request = new RestRequest("book", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");
            var title = "Pride and Prejudice";
            var author = "Jane Austen";
            var description = "A beautiful book";
            var price = 12.80;
            var pages = 500;

            request.AddJsonBody(new
            {
                title,
                author,
                description,
                price,
                pages,
                category = categoryId
            });

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");
            });

            var createdBook = JObject.Parse(response.Content);
            Assert.That(createdBook["_id"]?.ToString(), Is.Not.Empty, "Created book didn't have an Id.");

            var createdBookId = createdBook["_id"].ToString();

            var getBookRequest = new RestRequest("book/{id}", Method.Get);
            getBookRequest.AddUrlSegment("id", createdBookId);
            var getBookResponse = client.Execute(getBookRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getBookResponse.Content, Is.Not.Empty, "Response content should not be empty");

                var content = JObject.Parse(getBookResponse.Content);

                Assert.That(content["title"]?.ToString(), Is.EqualTo(title), "Book title should match the input.");
                Assert.That(content["author"]?.ToString(), Is.EqualTo(author), "Book author should match the input.");
                Assert.That(content["description"]?.ToString(), Is.EqualTo(description), "Book description should match the input.");
                Assert.That(content["price"]?.Value<double>(), Is.EqualTo(price), "Book price should match the input.");
                Assert.That(content["pages"]?.Value<int>(), Is.EqualTo(pages), "Book pages should match the input.");

                Assert.That(content["category"]?.ToString(), Is.Not.Null.And.Not.Empty, "Book category should not be null or empty");
                Assert.That((string)content["category"]["_id"], Is.EqualTo(categoryId), $"Book category should be '{categoryId}'");

            });

        }

        [Test]
        public void Test_UpdateBook()
        {
            var getRequest = new RestRequest("book", Method.Get);

            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Failed to retrieve books");
            Assert.That(getResponse.Content, Is.Not.Empty, "Get books response content is empty");

            var books = JArray.Parse(getResponse.Content);
            var bookToUpdate = books.FirstOrDefault(b => b["title"]?.ToString() == "The Catcher in the Rye");

            Assert.That(bookToUpdate, Is.Not.Null, "Book with name 'The Catcher in the Rye' not found");

            var bookId = bookToUpdate["_id"].ToString();

            var updateRequest = new RestRequest("book/{id}", Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddUrlSegment("id", bookId);
            updateRequest.AddJsonBody(new
            {
                title = "The Catcher in the Rye 2",
                author = "J. David Salinger",
            });

            var updateResponse = client.Execute(updateRequest);

            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Empty, "Update response content should not be empty");

                var content = JObject.Parse(updateResponse.Content);

                Assert.That(content["title"]?.ToString(), Is.EqualTo("The Catcher in the Rye 2"), "Book name should match the updated value");
                Assert.That(content["author"]?.ToString(), Is.EqualTo("J. David Salinger"), "Book author should match the updated value");
            });
        }

        [Test]
        public void Test_DeleteBook()
        {
            var getRequest = new RestRequest("book", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Failed to retrieve books");
            Assert.That(getResponse.Content, Is.Not.Empty, "Get book response content is empty");

            var books = JArray.Parse(getResponse.Content);
            var bookToDelete = books.FirstOrDefault(b => b["title"]?.ToString() == "To Kill a Mockingbird");

            Assert.That(bookToDelete, Is.Not.Null, "Book with name 'To Kill a Mockingbird' not found");

            var bookId = bookToDelete["_id"].ToString();

            var deleteRequest = new RestRequest("book/{id}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");
            deleteRequest.AddUrlSegment("id", bookId);

            var deleteResponse = client.Execute(deleteRequest);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code Ok");

                var verifyGetRequest = new RestRequest("book/{id}", Method.Get);
                verifyGetRequest.AddUrlSegment("id", bookId);

                var verifyGetResponse = client.Execute(verifyGetRequest);

                Assert.That(verifyGetResponse.Content, Is.Null.Or.EqualTo("null"), "Verify get response content should be empty");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
