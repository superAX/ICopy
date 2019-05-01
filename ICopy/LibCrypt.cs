using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

//  AES Encryption 
public class CAesCrypt
{
    RijndaelManaged aes = new RijndaelManaged();

    public CAesCrypt()
    {
        byte[] key = { 221, 124, 171, 19, 245, 126, 173, 99, 2, 53, 174, 41, 89, 5, 17, 82, 219, 55, 30, 110, 74, 28, 161, 34, 48, 150, 91, 230, 72, 211, 138, 23 };    
        byte[] iv = { 74, 58, 13, 6, 112, 214, 99, 42, 71, 66, 157, 209, 36, 248, 118, 172 };
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;
    }

    // Create a pair of random new KEY and IV
    public void CreateNewKey()
    {
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();
    }

    /// Encrypt a file by AES 256
    public void EncryptFile(string srcFile, string tarFile)
    {
        using (FileStream fs1 = new FileStream(srcFile, FileMode.Open), fs2 = new FileStream(tarFile, FileMode.Create))
        {
            using (CryptoStream cs = new CryptoStream(fs2, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] buf = new byte[1024 * 1024];
                int length;

                while ((length = fs1.Read(buf, 0, buf.Length)) > 0)
                {
                    cs.Write(buf, 0, length);
                }

                cs.FlushFinalBlock();
                cs.Close();
            }
            fs1.Close();
            fs2.Close();
        }
    }
    
    // Decrypt a AES 256 file
    public void DecryptFile(string srcFile, string tarFile)
    {
        using (FileStream fs1 = new FileStream(srcFile, FileMode.Open), fs2 = new FileStream(tarFile, FileMode.Create))
        {
            using (CryptoStream cs = new CryptoStream(fs1, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                byte[] buf = new byte[1024 * 1024];
                int length;

                while ((length = cs.Read(buf, 0, buf.Length)) > 0)
                {
                    fs2.Write(buf, 0, length);
                }
                cs.Close();
            }
            fs1.Close();
            fs2.Close();
        }
    }
}
