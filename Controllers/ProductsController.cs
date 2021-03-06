﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MySql.Data.MySqlClient;
using Renci.SshNet.Security;
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
        public async Task<ActionResult<SearchResultDTO>> SearchResults(string searchType, string searchQuery, [FromQuery] string[] filter = null)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();
            SearchResultDTO result = new SearchResultDTO();
            // Handle product list
            if (searchType == "filter" && (filter != null && filter.Length > 0))
            {
                result.products = await ListFilteredProducts(searchQuery, filter);
            } else
            {
                result.products = await ListProducts(searchType, searchQuery);
            }
            

            // Handle category list
            if (searchType == "preview")
            {
                cmd.CommandText = @"SELECT DISTINCT ProductCategory FROM ProductsTable WHERE ProductCategory LIKE CONCAT('%', @Category, '%');";
                cmd.Parameters.AddWithValue("@Category", searchQuery);

                MySqlDataReader dataReader = await Task.Run(() => cmd.ExecuteReader());
                List<string> categoryList = CreateCategoryList(dataReader);
                result.categories = categoryList;
            } else 
            {
                result.categories = await ListFilters(searchQuery, filter);
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

            if ((searchType == "string") || (searchType == "preview") || (searchType == "filter"))
            {
                cmd.CommandText = @"SELECT * FROM ProductsTable WHERE ProductName LIKE CONCAT('%', @Query, '%');";
                cmd.Parameters.AddWithValue("@Query", searchQuery);

            }
            else if (searchType == "category")
            {
                cmd.CommandText = @"SELECT * FROM ProductsTable WHERE ProductCategory = @Category;";
                cmd.Parameters.AddWithValue("@Category", searchQuery);
            }
            // else with default value or error message
            MySqlDataReader dataReader = await Task.Run(() => cmd.ExecuteReader());
            return CreateProductList(dataReader);
        }

        private async Task<IEnumerable<Product>> ListFilteredProducts(string searchQuery, string[] filter)
        {
            FilterContainer filterContainer = new FilterContainer(filter);
            var cmd = this.MySqlDatabase.Connection.CreateCommand();

            cmd.CommandText = @"SELECT * FROM ProductsTable WHERE ";
            if(searchQuery != null)
            {
                cmd.CommandText += "ProductName LIKE CONCAT('%', '" + searchQuery + "' , '%') AND ";
            }

            int counter = 0;
            foreach(var entry in filterContainer.FilterLookup)
            {
                if (counter > 0)
                {
                    cmd.CommandText += " AND ";
                }
                cmd.CommandText += AddFilter(entry);
                counter++;
            }
            cmd.CommandText += ";";
            MySqlDataReader dataReader = await Task.Run(() => cmd.ExecuteReader());
            return CreateProductList(dataReader);
        }

        private async Task<IEnumerable<string>> ListFilters(string searchQuery, string[] filters)
        {
            var cmd = this.MySqlDatabase.Connection.CreateCommand();
            cmd.CommandText = @"SELECT DISTINCT ProductCategory FROM ProductsTable WHERE ProductName LIKE CONCAT('%', @Query, '%');";
            cmd.Parameters.AddWithValue("@Query", searchQuery);

            MySqlDataReader dataReader = await Task.Run(() => cmd.ExecuteReader());
            return CreateCategoryList(dataReader);
        }

        private string AddFilter(IGrouping<string, string> entry)
        {
            switch (entry.Key)
            {
                case "category":
                    string ret = "(";
                    int counter = 0;
                    foreach (string str in entry)
                    {
                        if ( counter > 0)
                        {
                            ret += " OR ";
                        }
                        ret += "ProductCategory = '" + str + "'";
                        counter++;
                    }
                    return ret += ")";
                case "stock":
                    return "ProductStock > 0";
                default:
                    return "";
            }
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