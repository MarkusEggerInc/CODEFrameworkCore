using CODE.Framework.Services.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CODE.Framework.Services.Contracts;

namespace Sample.Contracts
{    
    public interface IUserService
    {     
        [Rest(Method = RestMethods.Get, Route = "signout")]
        SignoutResponse Signout(SignoutRequest request);

        [Rest(Method = RestMethods.Post, Route = "isauthenticated")]
        IsAuthenticatedResponse IsAuthenticated(IsAuthenticatedRequest request);
        
        [Rest(Method = RestMethods.Post, Route = "user")]
        SaveUserResponse SaveUser(SaveUserRequest request);

        [Rest(Method = RestMethods.Post, Route = "resetpassword")]
        ResetPasswordResponse ResetPassword(ResetPasswordRequest request);

        [Rest(Method = RestMethods.Get, Route = "{id:guid}")]
        GetUserResponse GetUser(GetUserRequest request);

        [Rest(Method = RestMethods.Post, Route = "authenticate")]
        AuthenticateUserResponse AuthenticateUser(AuthenticateUserRequest request);

        [Rest(Method = RestMethods.Get, Route = "")]
        GetUsersResponse GetUsers(GetUsersRequest request);        
    }




    public class GetUserRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class GetUserResponse : BaseServiceResponse
    {
        public GetUserResponse()
        {
            Roles = new List<string>();
        }

        [DataMember(IsRequired = true)]
        public Guid UserId { get; set; }

        [DataMember(IsRequired = true)]
        public string Username { get; set; }

        [DataMember(IsRequired = true)]
        public string Email { get; set; }


        [DataMember(IsRequired = true)]
        public string Firstname { get; set; }

        [DataMember(IsRequired = true)]
        public string Lastname { get; set; }

        [DataMember(IsRequired = true)]
        public string Company { get; set; }

        [DataMember]
        public bool IsActive { get; set; }

        [DataMember]
        public string Verifier { get; set; }

        [DataMember]
        public DateTime LastLogin { get; set; }

        [DataMember]
        public List<string> Roles { get; set; }

    }

    [DataContract]
    public class GetUsersRequest
    {
    }

    [DataContract]
    public class GetUsersResponse : BaseServiceResponse
    {
        [DataMember]
        public List<User> Users = new List<User>();
    }

    public class SignoutRequest : BaseServiceRequest
    {

    }

    [DataContract]
    public class ResetPasswordRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        public string Username { get; set; }
    }

    [DataContract]
    public class ResetPasswordResponse : BaseServiceResponse
    {
    }


    public class SignoutResponse : BaseServiceResponse
    {
    }

    public class IsAuthenticatedRequest : BaseServiceRequest
    {

    }

    public class IsAuthenticatedResponse : BaseServiceResponse
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string UserId { get; set; }
    }


    public class AuthenticateUserRequest : BaseServiceRequest
    {
        public AuthenticateUserRequest()
        {
            UserName = string.Empty;
            //Password = string.Empty;
            RememberMe = false;
        }

        [DataMember(IsRequired = true)]
        public string UserName { get; set; }
        [DataMember(IsRequired = true)]
        public string Password { get; set; }
        [DataMember(IsRequired = true)]
        public bool RememberMe { get; set; }
    }

    [DataContract]
    public class AuthenticateUserResponse : BaseServiceResponse
    {
        public AuthenticateUserResponse()
        {
            Success = true;
            FailureInformation = string.Empty;
            Roles = new List<string>();
        }

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Firstname { get; set; }

        [DataMember]
        public string Lastname { get; set; }

        [DataMember]
        public string Company { get; set; }

        [DataMember]
        public List<string> Roles { get; set; }
    }

    [DataContract]
    public class SaveUserRequest
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string UserId { get; set; }

        [DataMember(IsRequired = true)]
        public string Username { get; set; }

        [DataMember]
        public string Email { get; set; }


        [DataMember(IsRequired = true)]
        public string Firstname { get; set; }

        [DataMember(IsRequired = true)]
        public string Lastname { get; set; }

        [DataMember]
        public string Company { get; set; }

        [DataMember]
        public bool IsActive { get; set; }

        [DataMember]
        public string Verifier { get; set; }

        [DataMember]
        public DateTime LastLogin { get; set; }

        [DataMember]
        public List<string> Roles { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string PasswordVerify { get; set; }
    }

    [DataContract]
    public class SaveUserResponse
    {
        /// <summary>
        /// Gets or sets the success status
        /// </summary>        
        [DataMember]
        public bool Success { get; set; }

        /// <summary>
        /// Error message if an error occurred and Success=false
        /// </summary>
        [DataMember]
        public string FailureInformation { get; set; }

        /// <summary>
        /// Id of the updated or new User
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; }
        public string Password { get; set; }

    }

}
