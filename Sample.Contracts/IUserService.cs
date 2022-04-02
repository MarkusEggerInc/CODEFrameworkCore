using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using CODE.Framework.Services.Contracts;

namespace Sample.Contracts
{   
    [ServiceContract]
    [Summary("Services/API related to user management")]
    [Description("This service includes all kinds of operations related to user management, such as login/logout, user CRUD operations, and so on.")]
    [ExternalDocumentation("For more information, please refer to the external documentation.", "https://docs.codeframework.io")]
    public interface IUserService
    {     
        [Rest(Method = RestMethods.Get)]
        [Summary("Signout/logout user")]
        [Description("This method logs the specified user out of the system and returns information indicating `success` or `failure`.")]
        SignoutResponse Signout(SignoutRequest request);

        [Rest(Method = RestMethods.Post)]
        IsAuthenticatedResponse IsAuthenticated(IsAuthenticatedRequest request);
        
        [Rest(Method = RestMethods.Put, Name = "user")]
        SaveUserResponse SaveUser(SaveUserRequest request);

        [Rest(Method = RestMethods.Post)]
        ResetPasswordResponse ResetPassword(ResetPasswordRequest request);

        [Rest(Method = RestMethods.Get, Name = "user")]
        GetUserResponse GetUser(GetUserRequest request);

        [Rest(Method = RestMethods.Get, Name = "authenticate")]
        [Summary("Authenticates a user.")]
        [Description("Authenticates a user, based on user name and password. If successful, returns the user's `Id`.")]
        AuthenticateUserResponse AuthenticateUser(AuthenticateUserRequest request);

        [Rest(Method = RestMethods.Get, Route = "")]
        [Description("Retrieves a list of all users.")]
        GetUsersResponse GetUsers(GetUsersRequest request);

        [Rest(Method = RestMethods.Get)]
        DateTime Test(string bla);
    }

    [DataContract]
    [Description("Request to query a specific user")]
    public class GetUserRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        [RestUrlParameter(Mode = UrlParameterMode.Inline)]
        [Description("Unique User ID (formatted as a globally unique ID, a.k.a. GUID)")]
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    [DataContract]
    [Description("Response with user detail.")]
    public class GetUserResponse : BaseServiceResponse
    {
        [DataMember(IsRequired = true)]
        [Description("Unique User ID (formatted as a globally unique ID, a.k.a. GUID)")]
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

    /// <summary>
    /// Request message for password reset operations
    /// </summary>
    [DataContract]
    public class ResetPasswordRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response message for password reset operations
    /// </summary>
    [DataContract]
    public class ResetPasswordResponse : BaseServiceResponse { }

    [DataContract]
    public class SignoutResponse : BaseServiceResponse { }

    [DataContract]
    public class IsAuthenticatedRequest : BaseServiceRequest { }

    [DataContract]
    public class IsAuthenticatedResponse : BaseServiceResponse
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string UserId { get; set; }
    }

    [DataContract]
    public class AuthenticateUserRequest : BaseServiceRequest
    {
        [DataMember(IsRequired = true)]
        [RestUrlParameter(Mode = UrlParameterMode.Inline, Sequence = 0)]
        [Description("User name (usually an email address).")]
        public string UserName { get; set; } = string.Empty;

        [DataMember(IsRequired = true)]
        [RestUrlParameter(Mode = UrlParameterMode.Inline, Sequence = 1)]
        [Description("Password (clear text) - ***Should only ever be called over HTTPS!!!***")]
        public string Password { get; set; } = string.Empty;

        [DataMember(IsRequired = false)]
        [RestUrlParameter(Mode = UrlParameterMode.Named)]
        [Description("Boolean flag, indicating whether the user should be remembered. (*False by default*)")]
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
        [DataMember, RestUrlParameter(Mode = UrlParameterMode.Inline)]
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

    [DataContract]
    public class User
    {
        [DataMember]
        public Guid Id { get; set; } = Guid.NewGuid();

        [DataMember]
        public string Username { get; set; } = string.Empty;

        [DataMember]
        public string Password { get; set; } = string.Empty;
    }
}
