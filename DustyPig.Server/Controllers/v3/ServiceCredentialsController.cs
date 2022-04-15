﻿using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "Service Credentials")]
    [ExceptionLogger(typeof(ServiceCredentialsController))]
    [SwaggerResponse((int)HttpStatusCode.Forbidden)]
    public class ServiceCredentialsController : _BaseProfileController
    {
        public ServiceCredentialsController(AppDbContext db) : base(db) { }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<API.v3.Models.ServiceCredential>>> List()
        {
            var creds = await DB.EncryptedServiceCredentials
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .ToListAsync();

            return creds.OrderBy(item => item.Name).Select(item => new API.v3.Models.ServiceCredential
            {
                Id = item.Id,
                CredentialType = item.CredentialType,
                Name = item.Name
            }).ToList();
        }

        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! This will delete all media where either the ArtworkServiceCredential, BifServiceCredential or VideoServiceCredential is linked to this.  It will also delete any subtitles where the Credential is linked.  It will NOT remove media where only subtitles are linked.</remarks>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> Delete(int id)
        {
            var cred = await DB.EncryptedServiceCredentials
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();


            if (cred != null)
            {
                DB.EncryptedServiceCredentials.Remove(cred);
                await DB.SaveChangesAsync();
            }

            return Ok();
        }





        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<S3Credential>> GetS3Details(int id)
        {
            var cred = await DB.EncryptedServiceCredentials
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (cred == null)
                return NotFound();

            if (cred.CredentialType != ServiceCredentialTypes.S3)
                return BadRequest($"Credential {id} is not a {nameof(S3Credential)}");

            return cred.Decrypt<S3Credential>();
        }

        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SimpleValue<int>>> CreateS3(CreateS3Credential credential)
        {
            //Validate
            try { credential.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            //Ensure Unique
            var existing = await DB.EncryptedServiceCredentials
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Name == credential.Name)
                .AnyAsync();
            if (existing)
                return BadRequest("A credential with the specified name already exists in this account");


            //Create
            var newItem = new EncryptedServiceCredential
            {
                AccountId = UserAccount.Id,
                CredentialType = ServiceCredentialTypes.S3,
                Name = credential.Name
            };
            newItem.Encrypt(credential);

            DB.EncryptedServiceCredentials.Add(newItem);
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(newItem.Id));
        }

        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> UpdateS3(S3Credential credential)
        {
            //Validate
            try { credential.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            var existing = await DB.EncryptedServiceCredentials
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == credential.Id)
                .FirstOrDefaultAsync();
            if (existing == null)
                return NotFound();

            if (existing.CredentialType != ServiceCredentialTypes.S3)
                return BadRequest($"Credential {credential.Id} is not a {nameof(S3Credential)}");

            //Check for unique
            if (credential.Name != existing.Name)
            {
                var dup = await DB.EncryptedServiceCredentials
                    .AsNoTracking()
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Where(item => item.Id != credential.Id)
                    .Where(item => item.Name == credential.Name)
                    .AnyAsync();
                if (dup)
                    return BadRequest($"Another credential in this account already has the specified {nameof(credential.Name)}");
            }


            //Update
            existing.Name = credential.Name;
            existing.Encrypt(credential);
            await DB.SaveChangesAsync();

            return Ok();
        }









        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<GoogleDriveCredential>> GetGoogleDriveDetails(int id)
        {
            var cred = await DB.EncryptedServiceCredentials
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (cred == null)
                return NotFound();

            if (cred.CredentialType != ServiceCredentialTypes.GoogleDriveServiceAccount)
                return BadRequest($"Credential {id} is not a {nameof(GoogleDriveCredential)}");

            return cred.Decrypt<GoogleDriveCredential>();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SimpleValue<int>>> CreateGoogleDrive(CreateGoogleDriveCredential credential)
        {
            try { credential.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }




            //Ensure Unique
            var existing = await DB.EncryptedServiceCredentials
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Name == credential.Name)
                .AnyAsync();
            if (existing)
                return BadRequest("A credential with the specified name already exists in this account");


            //Create
            var newItem = new EncryptedServiceCredential
            {
                AccountId = UserAccount.Id,
                CredentialType = ServiceCredentialTypes.GoogleDriveServiceAccount,
                Name = credential.Name,
                Data = JsonConvert.SerializeObject(credential)
            };

            DB.EncryptedServiceCredentials.Add(newItem);
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(newItem.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> UpdateGoogleDrive(GoogleDriveCredential credential)
        {
            try { credential.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            var existing = await DB.EncryptedServiceCredentials
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == credential.Id)
                .FirstOrDefaultAsync();
            if (existing == null)
                return NotFound();

            if (existing.CredentialType != ServiceCredentialTypes.GoogleDriveServiceAccount)
                return BadRequest($"Credential {credential.Id} is not a {nameof(GoogleDriveCredential)}");

            //Check for unique
            if (credential.Name != existing.Name)
            {
                var dup = await DB.EncryptedServiceCredentials
                    .AsNoTracking()
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Where(item => item.Id != credential.Id)
                    .Where(item => item.Name == credential.Name)
                    .AnyAsync();
                if (dup)
                    return BadRequest($"Another credential in this account already has the specified {nameof(credential.Name)}");
            }


            //Update
            existing.Name = credential.Name;
            existing.Encrypt(credential);
            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<GoogleDriveToken>> GetGoogleDriveToken(int id)
        {
            var cred = await DB.EncryptedServiceCredentials
                .AsNoTracking()
                .Where(item => item.AccountId != UserAccount.Id)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (cred == null)
                return NotFound();

            if (cred.CredentialType != ServiceCredentialTypes.GoogleDriveServiceAccount)
                return BadRequest($"The specified credential is not a {nameof(GoogleDriveCredential)}");

            var decryptedGD = cred.Decrypt<GoogleDriveCredential>();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential
                    .FromJson(decryptedGD.ServiceCredentialsJson)
                    .CreateScoped(new string[] { DriveService.Scope.Drive })
                    .CreateWithUser(decryptedGD.Email),
                ApplicationName = "Dusty Pig"
            });
            string tokenVal = ((ICredential)service.HttpClientInitializer).GetAccessTokenForRequestAsync().Result;

            return new GoogleDriveToken
            {
                Token = ((ICredential)service.HttpClientInitializer).GetAccessTokenForRequestAsync().Result,
                ExpiresUTC = DateTime.UtcNow.AddMinutes(59)
            };
        }


    }
}