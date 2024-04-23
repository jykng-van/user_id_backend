using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace user_id_backend.Controllers
{
    [AllowAnonymous]
    [Produces("application/json")]
    public class AvatarController : Controller
    {
        public async Task<IActionResult> Index([FromQuery] string user_identifier)
        {
            try
            {
                user_identifier = user_identifier.ToLower(); //make lower case
                string url = "https://api.dicebear.com/8.x/pixel-art/png?seed=default&size=150"; ;

                var upper_end_pattern = @"[6-9]$";
                var upper_end_match = Regex.Match(user_identifier, upper_end_pattern);

                var lower_end_pattern = @"[1-5]$";
                var lower_end_match = Regex.Match(user_identifier, lower_end_pattern);

                var one_vowel = @"[aeiou]";

                var non_alphanumeric = @"[^a-zA-Z\d]";

                /* 
                 * If the last character of the user identifier is [6, 7, 8, 9], retrieve the image URL from https://my-json-server.typicode.com/ck-pacificdev/tech-test/images/{lastDigitOfUserIdentifier} 
                 * where {lastDigitOfUserIdentifier} is the last digit of the identifier
                 */

                if (upper_end_match.Success)
                {
                    //url = "https://my-json-server.typicode.com/ck-pacificdev/tech-test/images/" + upper_end_match.Value;
                    var retrieve_url = "https://my-json-server.typicode.com/ck-pacificdev/tech-test/images/" + upper_end_match.Value;
                    var client = new HttpClient();                    
                    var response = await client.GetFromJsonAsync<AvatarImage>(retrieve_url);
                    if (response != null)
                    {
                        url = response.Url;
                    }
                    
                }
                /* 
                 * If the user last character of the user identifier is [1, 2, 3, 4, 5], retrieve the image URL from the data.db Sqlite database where the images.id value matches the last digit of the identifier
                 */
                else if (lower_end_match.Success)
                {
                    using (var connection = new SqliteConnection("Data Source=data.db"))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "SELECT url FROM images WHERE id=$id";
                        command.Parameters.AddWithValue("$id", Convert.ToInt32(lower_end_match.Value));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                url = reader.GetString(0);
                            }
                        }

                    }
                }
                /*
                 * If the user identifier contains at least one vowel character (aeiou), display the image from https://api.dicebear.com/8.x/pixel-art/png?seed=vowel&size=150
                 */
                else if (Regex.IsMatch(user_identifier, one_vowel))
                {
                    url = "https://api.dicebear.com/8.x/pixel-art/png?seed=vowel&size=150";
                }
                /*
                 * If the user identifier contains a non-alphanumeric character, pick a random number between 1-5 and display the image with the appropriate seed 
                 * (e.g. https://api.dicebear.com/8.x/pixel-art/png?seed={randomNumber}&size=150)
                 */
                else if (Regex.IsMatch(user_identifier, non_alphanumeric))
                {
                    var rand = new Random();
                    var num = rand.Next(1, 6);
                    url = $"https://api.dicebear.com/8.x/pixel-art/png?seed={num}&size=150";
                }
                /* If none of the above conditions are met, display the image from https://api.dicebear.com/8.x/pixel-art/png?seed=default&size=150 */
                else
                {
                    //see above for default value of url
                }
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        public class AvatarImage
        {
            public int Id { get; set; }
            public string Url { get; set; } = "";
        }
    }
}
