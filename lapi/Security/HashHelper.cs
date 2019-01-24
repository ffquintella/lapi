using System;
using System.Security.Cryptography;
using System.Text;

namespace lapi.Security
{
    public static class HashHelper
    {
        public static string GenerateSaltedSHA1(string plainTextString)
        {
            HashAlgorithm algorithm = new SHA1Managed();
            var saltBytes = GenerateSalt(4);
            var plainTextBytes = Encoding.ASCII.GetBytes(plainTextString);

            var plainTextWithSaltBytes = AppendByteArray(plainTextBytes, saltBytes);
            var saltedSha1Bytes = algorithm.ComputeHash(plainTextWithSaltBytes);
            byte[] saltedSha1WithAppendedSaltBytes = AppendByteArray(saltedSha1Bytes, saltBytes);
           
            return "{SSHA}" + Convert.ToBase64String(saltedSha1WithAppendedSaltBytes);
        } 

        
        private static byte[] GenerateSalt(int saltSize)
        {
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[saltSize];
            rng.GetBytes(buff);
            return buff; 
        }
        
        private static byte[] AppendByteArray(byte[] byteArray1, byte[] byteArray2)
        {
            var byteArrayResult =
                new byte[byteArray1.Length + byteArray2.Length];

            for (var i = 0; i < byteArray1.Length; i++)
                byteArrayResult[i] = byteArray1[i];
            for (var i = 0; i < byteArray2.Length; i++)
                byteArrayResult[byteArray1.Length + i] = byteArray2[i];

            return byteArrayResult;
        }
    }
}