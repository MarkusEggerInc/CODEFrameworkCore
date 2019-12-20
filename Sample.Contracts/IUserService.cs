using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using CODE.Framework.Services.Contracts;

namespace Sample.Contracts
{   
    [ServiceContract]
    public interface IUserService
    {     
        [Rest(Method = RestMethods.Get)]
        SignoutResponse Signout(SignoutRequest request);

        [Rest(Method = RestMethods.Post)]
        IsAuthenticatedResponse IsAuthenticated(IsAuthenticatedRequest request);
        
        [Rest(Method = RestMethods.Post, Name = "user")]
        SaveUserResponse SaveUser(SaveUserRequest request);

        [Rest(Method = RestMethods.Post)]
        ResetPasswordResponse ResetPassword(ResetPasswordRequest request);

        [Rest(Method = RestMethods.Get, Name = "")]
        GetUserResponse GetUser(GetUserRequest request);

        [Rest(Method = RestMethods.Get, Name = "authenticate")]
        AuthenticateUserResponse AuthenticateUser(AuthenticateUserRequest request);

        [Rest(Method = RestMethods.Get, Route = "")]
        GetUsersResponse GetUsers(GetUsersRequest request);        
    }

    public class GetUserRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true), RestUrlParameter(Mode = UrlParameterMode.Inline)]
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class GetUserResponse : BaseServiceResponse
    {
        [DataMember(IsRequired = true)]
        public Guid UserId { get; set; } = Guid.Empty;

        [DataMember(IsRequired = true)]
        public string Username { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string Email { get; set; } = string.Empty;


        [DataMember(IsRequired = true)]
        public string Firstname { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string Lastname { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string Company { get; set; } = string.Empty;

        [DataMember]
        public bool IsActive { get; set; } = true;

        [DataMember]
        public string Verifier { get; set; } = string.Empty;

        [DataMember]
        public DateTime LastLogin { get; set; } = DateTime.MinValue;

        [DataMember]
        public List<string> Roles { get; set; } = new List<string>();
    }

    [DataContract]
    public class GetUsersRequest { }

    [DataContract]
    public class GetUsersResponse : BaseServiceResponse
    {
        [DataMember]
        public List<User> Users { get; set;  } = new List<User>();
    }

    public class SignoutRequest : BaseServiceRequest { }

    [DataContract]
    public class ResetPasswordRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        public string Username { get; set; } = string.Empty;
    }

    [DataContract]
    public class ResetPasswordResponse : BaseServiceResponse { }

    public class SignoutResponse : BaseServiceResponse { }

    public class IsAuthenticatedRequest : BaseServiceRequest { }

    public class IsAuthenticatedResponse : BaseServiceResponse
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string UserId { get; set; }
    }

    public class AuthenticateUserRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        [RestUrlParameter(Mode = UrlParameterMode.Inline, Sequence = 0)]
        public string UserName { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        [RestUrlParameter(Mode = UrlParameterMode.Inline, Sequence = 1)]
        public string Password { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public bool RememberMe { get; set; } = false;
    }

    [DataContract]
    public class AuthenticateUserResponse : BaseServiceResponse
    {
        [DataMember]
        public Guid Id { get; set; } = Guid.Empty;

        [DataMember]
        public string Email { get; set; } = string.Empty;

        [DataMember]
        public string Firstname { get; set; } = string.Empty;

        [DataMember]
        public string Lastname { get; set; } = string.Empty;

        [DataMember]
        public string Company { get; set; } = string.Empty;

        [DataMember]
        public List<string> Roles { get; set; } = new List<string>();
    }

    [DataContract]
    public class SaveUserRequest
    {
        [DataMember]
        public Guid Id { get; set; } = Guid.Empty;

        [DataMember]
        public string UserId { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string Username { get; set; } = string.Empty;

        [DataMember]
        public string Email { get; set; } = string.Empty;


        [DataMember(IsRequired = true)]
        public string Firstname { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        public string Lastname { get; set; } = string.Empty;

        [DataMember]
        public string Company { get; set; } = string.Empty;

        [DataMember]
        public bool IsActive { get; set; } = true;

        [DataMember]
        public string Verifier { get; set; } = string.Empty;

        [DataMember]
        public DateTime LastLogin { get; set; } = DateTime.MinValue;

        [DataMember]
        public List<string> Roles { get; set; } = new List<string>();

        [DataMember]
        public string Password { get; set; } = string.Empty;

        [DataMember]
        public string PasswordVerify { get; set; } = string.Empty;
    }

    [DataContract]
    public class SaveUserResponse : BaseServiceResponse
    {
        /// <summary>
        /// Id of the updated or new User
        /// </summary>
        [DataMember]
        public Guid Id { get; set; } = Guid.Empty;
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
