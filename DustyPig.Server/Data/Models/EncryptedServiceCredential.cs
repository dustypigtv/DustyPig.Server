using DustyPig.API.v3.Models;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    /// <summary>
    /// Credentials for services needed to generate playback urls. For example, Amazon S3 or Google Drive
    /// </summary>
    [Index(nameof(AccountId), nameof(Name), IsUnique = true)]
    public class EncryptedServiceCredential
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        [Required]
        public ServiceCredentialTypes CredentialType { get; set; }

        /// <summary>
        /// Encrypted serialized json object.
        /// </summary>
        [Required]
        public string Data { get; set; }



        public T Decrypt<T>() => JsonConvert.DeserializeObject<T>(Crypto.Decrypt(Data));

        public void Encrypt(object sc) => Data = Crypto.Encrypt(JsonConvert.SerializeObject(sc));
    }
}
