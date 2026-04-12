using System;
using System.Security.Cryptography;
using System.Text;

class HashGenerator
{
    static void Main()
    {
        string password = "kibepaul";
        string salt = "x5fc218c3707a721c71c2c58b5771ba39e92f919d044241da43ac8ff149c5c991355d34624b5f3f7d52c9124c4b7f3b1de106840aa7727eda855eea76a54f9e8af8859f9e1449f38b9e15992814001c31c9965674f12e9317615a975d34f9e1af55de3dca3bd34b18bfd9d00bf14b2fb03d01e11a176ce7272725537d868614bcB";
        
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        
        using (var hmac = new HMACSHA512(saltBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(passwordBytes);
            string hashString = Convert.ToBase64String(hashBytes);
            Console.WriteLine($"Hash: {hashString}");
        }
    }
}
