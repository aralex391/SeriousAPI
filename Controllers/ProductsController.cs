using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MySql.Data.MySqlClient;
using SeriousAPI.Models;

namespace SeriousAPI.Controllers
{
    [Route("api/Products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private MySqlDatabase MySqlDatabase { get; set; }
        public ProductsController(MySqlDatabase mySqlDatabase)
        {
            this.MySqlDatabase = mySqlDatabase;
        }

        [HttpGet]
        public async Task<ActionResult<SearchResultDTO>> ListResults(string searchType, string searchQuery)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();
            SearchResultDTO result = new SearchResultDTO();

            // Handle product list
            result.products = await ListProducts(searchType, searchQuery);

            // Handle category list
            if (searchType == "preview")
            {
                cmd.CommandText = @"SELECT DISTINCT ProductCategory FROM ProductsTable WHERE ProductCategory=@Category;";
                cmd.Parameters.AddWithValue("@Category", searchQuery);

                MySqlDataReader dataReader = await Task.Run(() => cmd.ExecuteReader());
                List<string> categoryList = CreateCategoryList(dataReader);
                result.categories = categoryList;
            } else
            {
                result.categories = new List<string>();
            }
            
            return result;
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();

            cmd.CommandText = @"INSERT INTO ProductsTable(ProductCategory, ProductName, ProductPrice, ProductStock, ProductDescription)" +
                "VALUES(@Category, @Name, @Price, @Stock, @Description);";
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Stock", product.Stock);
            cmd.Parameters.AddWithValue("@Description", product.Description);

            await Task.Run(() => cmd.ExecuteNonQuery());
            return NoContent();
        }

        [HttpPatch] // Look up how to safe against repeat requests // ****UNUSED****
        public async Task<IActionResult> PatchProduct(int id, string field, string newValue)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();
            MySqlCommandBuilder cmdBuilder = new MySqlCommandBuilder();
            string fieldName = cmdBuilder.QuoteIdentifier(field);

            cmd.CommandText = @"UPDATE ProductsTable SET " + fieldName +  " = @NewValue WHERE ProductId = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@NewValue", newValue);

            await Task.Run(() => cmd.ExecuteNonQuery());
            return NoContent();
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProduct(Product product)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();

            cmd.CommandText = @"UPDATE ProductsTable SET ProductCategory=@Category, ProductName = @Name, ProductPrice = @Price, " + 
                "ProductStock = @Stock, ProductDescription = @Description WHERE ProductId = @Id";
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Stock", product.Stock);
            cmd.Parameters.AddWithValue("@Description", product.Description);
            cmd.Parameters.AddWithValue("@Id", product.Id);

            await Task.Run(() => cmd.ExecuteNonQuery());
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProductById(int id)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();

            cmd.CommandText = @"DELETE FROM ProductsTable WHERE ProductId = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);

            await Task.Run(() => cmd.ExecuteNonQuery());
            return NoContent();
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteProductByName(string name)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();

            cmd.CommandText = @"DELETE FROM ProductsTable WHERE ProductName = @Name;";
            cmd.Parameters.AddWithValue("@Name", name);

            await Task.Run(() => cmd.ExecuteNonQuery());
            return NoContent();
        }

        private async Task<IEnumerable<Product>> ListProducts(string searchType, string searchQuery)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();

            if ((searchType == "string") || (searchType == "preview"))
            {
                cmd.CommandText = @"SELECT * FROM ProductsTable WHERE ProductName=@Query;";
                cmd.Parameters.AddWithValue("@Query", searchQuery);

            }
            else if (searchType == "category")
            {
                cmd.CommandText = @"SELECT * FROM ProductsTable WHERE ProductCategory=@Category;";
                cmd.Parameters.AddWithValue("@Category", searchQuery);
            }

            MySqlDataReader dataReader = await Task.Run(() => cmd.ExecuteReader());
            return CreateProductList(dataReader);
        }

        private List<string> CreateCategoryList(MySqlDataReader dataReader)
        {
            List<string> categories = new List<string>();

            while (dataReader.Read())
            {
                categories.Add((string)dataReader["ProductCategory"]);
            }
            dataReader.Close();
            return categories;
        }

        private List<Product> CreateProductList(MySqlDataReader dataReader)
        {
            List<Product> products = new List<Product>();

            while (dataReader.Read())
            {
                Product product = new Product();
                product.Id = (int)dataReader["ProductId"];
                product.Category = (string)dataReader["ProductCategory"];
                product.Name = (string)dataReader["ProductName"];
                product.Price = (int)dataReader["ProductPrice"];
                product.Stock = (int)dataReader["ProductStock"];
                product.Name = (string)dataReader["ProductName"];
                if (dataReader["ProductDescription"].GetType() != typeof(DBNull))
                {
                    product.Description = (string)dataReader["ProductDescription"];
                } else
                {
                    product.Description = "";
                }

                products.Add(product);
            }
            dataReader.Close();
            return products;
        }
    }
}