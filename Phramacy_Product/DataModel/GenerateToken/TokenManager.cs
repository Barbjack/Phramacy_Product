using Newtonsoft.Json;
using Phramacy_Product.Views.DBMaster;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace Phramacy_Product.DataModel.GenerateToken
{
    public class TokenManager
    {
        private const int TokenDurationDays = 2;
        public enum TokenStatus
        {
            Success,
            Expired,
            Invalid,
            InternetError,
            UnknownError
        }
        public class TokenData
        {
            public string Token { get; set; }
            public DateTime ExpiryDate { get; set; }
        }

        public async Task<TokenStatus> GetOrRefreshToken(string mobile)
        {
            string filePath = GetTokenFilePath(mobile);
            if (File.Exists(filePath))
            {
                TokenData tokenData = ReadTokenFile(filePath);
                if (tokenData != null && tokenData.ExpiryDate > DateTime.Now)
                {
                    return TokenStatus.Success;
                }
            }
            return await CreateNewToken(mobile, filePath);
        }

        private string GetTokenFilePath(string mobile)
        {
            string fileName = $"{mobile.Replace("@", "_").Replace(".", "_")}.json";
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tokens", fileName);
        }

        private TokenData ReadTokenFile(string filePath)
        {
            try
            {
                string encryptedJson = File.ReadAllText(filePath);
                string decryptedJson = DBMasterConnection.Decrypt(encryptedJson);
                return JsonConvert.DeserializeObject<TokenData>(decryptedJson);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private void SaveTokenFile(string filePath, TokenData tokenData)
        {
            try
            {
                string json = JsonConvert.SerializeObject(tokenData, Formatting.Indented);
                string encryptedJson = DBMasterConnection.Encrypt(json);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                File.WriteAllText(filePath, encryptedJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving token file: {ex.Message}", "Error");
            }
        }
        private async Task<TokenStatus> CreateNewToken(string mobile, string filePath)
        {
            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    name = GlobalData.LoggedInUser,
                    mobile = mobile,
                    plan = "yearly",
                    duration = 365
                };
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync($"{ApiBaseUrl}create_license.php", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                    if (jsonResponse.success == true)
                    {
                        string newToken = jsonResponse.token;
                       // DateTime expiryDate = DateTime.Parse("2025-09-20");
                        DateTime expiryDate = DateTime.Now.AddDays(TokenDurationDays);

                        SaveTokenFile(filePath, new TokenData
                        {
                            Token = newToken,
                            ExpiryDate = expiryDate
                        });
                        return TokenStatus.Success;
                    }
                    else
                    {
                        return TokenStatus.Invalid;
                    }
                }
                catch (HttpRequestException)
                {
                    MessageBox.Show("Please connect to the internet.", "Connection Error");
                    return TokenStatus.InternetError;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unknown error occurred: {ex.Message}", "Error");
                    return TokenStatus.UnknownError;
                }
            }
        }
    }
}
