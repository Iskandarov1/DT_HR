namespace DT_HR.Api.Contracts;

/// <summary>
/// Contains the API endpoint routes.
/// </summary>
public static class ApiRoutes
{
	/// <summary>
	/// Contains the item routes.
	/// </summary>
	public static class Items
	{
		public const string ById = "{id:Guid}";
		public const string Update = "{id:Guid}";
		public const string Remove = "{id:Guid}";
		public const string Me = "me";
	}

	/// <summary>
	/// Contains the companies routes.
	/// </summary>
	public static class Companies
	{
		public const string Me = "me";
		public const string Licenses = "licenses";
		public const string Activities = "activities";
		public const string Experiences = "experiences";
		public const string Services = "services";
		public const string Employees = "employees";
		public const string Check = "check";
		public const string ByServiceId = "by-serviceId";
		public const string HasAccess = "has-access";
		public const string MarketUsers = "market-users";
		public const string NotMarketUsers = "not-market-users";
	}

	/// <summary>
	/// Contains the language routes.
	/// </summary>
	public static class Languages
	{
		public const string ById = "{id:Guid}";
		public const string Update = "{id:Guid}";
		public const string Remove = "{id:Guid}";
	}

	/// <summary>
	/// Contains the files routes.
	/// </summary>
	public static class Files
	{
		public const string Name = "name";
		public const string Upload = "file";
	}

	/// <summary>
	/// Contains the banner routes.
	/// </summary>
	public static class Banner
	{
		public const string Status = "{id:Guid}";
		public const string Public = "public";
	}

	/// <summary>
	/// Contains the translate routes.
	/// </summary>
	public static class Translate
	{
		public const string ByQuestionId = "question/{id:Guid}";
		public const string ByBannerId = "banner/{id:Guid}";
		public const string ByServiceId = "service/{id:Guid}";
	}

	/// <summary>
	/// Contains the category routes.
	/// </summary>
	public static class Category
	{
		public const string Service = "service";
		public const string GetServiceCategoriesByFilter = "service-categories-search";
		public const string Question = "question";
		public const string GetQuestionCategoriesByFilter = "question-categories-search";
		public const string GetSpecialistCategoriesByFilter = "specialist-categories-search";
		public const string GetChildrenQuestionCategoriesByParentId = "children-question-categories-by-parentId";
		public const string ByCatalogId = "by-catalogId";
		public const string IsTop = "is-top";
		public const string IsMain = "is-main";
		public const string MoveUp = "move-up";
		public const string MoveDown = "move-down";
	}

	/// <summary>
	/// Contains the user routes.
	/// </summary>
	public static class User
	{
		public const string Me = "me";
		public const string Licenses = "licenses";
		public const string Activities = "activities";
		public const string Experiences = "experiences";
		public const string Services = "services";
		public const string Specialization = "specialization";
		public const string Specializations = "specializations";
		public const string Check = "check";
	}

	/// <summary>
	/// Contains the specialization routes.
	/// </summary>
	public static class Specialization
	{
		public const string Add = "add";
		public const string ByParentId = "parent";
	}
	/// <summary>
	/// Contains the tenant specialization routes.
	/// </summary>
	public static class TenantSpecialization
	{
		public const string CreateSpecialization = "create-specialization";
		public const string CreateSpecializations = "create-specializations";
		public const string UpdateSpecialization = "update-specialization";
		public const string RemoveSpecialization = "remove-specialization/{id:Guid}";
	}
	/// <summary>
	/// Contains the moderation routes.
	/// </summary>
	public static class Moderation
	{
		public const string Services = "services";
		public const string ServiceById = "services/{id:Guid}";
		public const string Questions = "questions";
		public const string QuestionById = "questions/{id:Guid}";
		public const string TenantSpecialization = "license";
		public const string TenantSpecializationById = "license/{id:Guid}";

		public const string SendTenant = "send-tenant";
		public const string SendService = "send-service";
		public const string SendTenantSpecialization = "send-tenant-specialization";
		public const string SendQuestion = "send-question";
		public const string SendTemplate = "send-template";
		public const string SendTemplateWithMacros = "send-template-with-macros";


		public const string AcceptTenant = "accept-tenant";
		public const string AcceptService = "accept-service";
		public const string AcceptQuestion = "accept-question";
		public const string AcceptTenantSpecialization = "accept-tenant-specialization";
		public const string AcceptTemplate = "accept-template";
		public const string AcceptTemplateWithMacros = "accept-template-with-macros";

		public const string ServiceBlock = "block-service";
		public const string QuestionBlock = "block-question";
		public const string TenantSpecializationBlock = "block-tenant-specialization";
		public const string TemplateBlock = "block-template";
		public const string TemplateWithMacrosBlock = "block-template-with-macros";

		public const string ServiceCancel = "cancel-service";
		public const string QuestionCancel = "cancel-question";
		public const string TenantSpecializationCancel = "Cancel-tenant-specialization";
		public const string TemplateCancel = "cancel-template";
		public const string TemplateCancelWithMacros = "cancel-template-with-macros";

		public const string ServiceReject = "reject-service";
		public const string QuestionReject = "reject-question";
		public const string TenantSpecializationReject = "reject-tenant-specialization";
		public const string TemplateReject = "reject-template";
		public const string TemplateWithMacrosReject = "reject-template-with-macros";

		public const string ByTenant = "tenant";

		public const string CreateProtectedContract = "create-protected-contract";

		public const string Template = "template";
		public const string TemplateById = "template/{id:Guid}";

		public const string TemplateWithMacros = "template-with-macros";
		public const string TemplateWithMacrosById = "template-with-macros/{id:Guid}";
	}

	/// <summary>
	/// Contains the service routes.
	/// </summary>
	public static class Service
	{
		public const string CreateTenantService = "tenant-service";
		public const string GetByCatalogId = "by-catalogId";
		public const string GetMy = "my";
		public const string GetMyActive = "my-active";
		public const string GetMyInactive = "my-inactive";
		public const string GetMyModeration = "my-moderation";
		public const string GetMyCancelled = "my-cancelled";
		public const string GetMyBlocked = "my-blocked";
	}

	/// <summary>
	/// Contains the favorite routes.
	/// </summary>
	public static class Favorite
	{
		public const string Company = "company";
		public const string User = "user";
		public const string Service = "service";
	}

	/// <summary>
	/// Contains the question routes.
	/// </summary>
	public static class Question
	{
		public const string GetPurchasedQuestions = "purchased";
		public const string GetQuestionByCategoryId = "by-categoryId";
		public const string GetQuestionsByTenantId = "by-tenantId";
		public const string GetQuestionsFullAnswer = "full-answer/{id:Guid}";
		public const string Buy = "buy";
		public const string Encrypt = "encrypt";
		public const string GetQuestionAuthors = "authors";
		public const string GetMy = "my";
		public const string GetMyById = "my/{id:Guid}";
		public const string GetAdministratorById = "administrator";
	}

	/// <summary>
	/// Contains the question routes.
	/// </summary>
	public static class QuestionFeedback
	{
		public const string ByQuestionId = "by-questionId";
	}

	/// <summary>
	/// Contains the rating routes.
	/// </summary>
	public static class Rating
	{
		public const string Company = "company";
		public const string User = "user";
	}

	/// <summary>
	/// Contains the order.
	/// </summary>
	public static class Order
	{
		public const string Cancel = "cancel";
	}

	/// <summary>
	/// Contains the job position.
	/// </summary>
	public static class JobPosition
	{
		public const string All = "job-position";
		public const string ById = "job-position/{id:Guid}";
		public const string Create = "job-position";
		public const string Update = "job-position";
		public const string Remove = "job-position/{id:Guid}";
	}
	/// <summary>
	/// Contains the public authority types.
	/// </summary>
	public static class PublicAuthorityType
	{
		public const string All = "type";
		public const string ById = "type/{id:Guid}";
		public const string Create = "type";
		public const string Update = "type";
		public const string Remove = "type/{id:Guid}";
	}

	/// <summary>
	/// Contains the public authority.
	/// </summary>
	public static class PublicAuthority
	{
		public const string All = "public-authority";
		public const string ById = "public-authority/{id:Guid}";
		public const string Details = "public-authority/details/{id:Guid}";
		public const string Create = "public-authority";
		public const string Update = "public-authority";
		public const string Remove = "public-authority/{id:Guid}";
		public const string CreateEmployee = "public-authority/employee";
		public const string UpdateEmployee = "public-authority/employee";
		public const string RemoveEmployee = "public-authority/employee/{id:Guid}";
	}

	/// <summary>
	/// Contains the public authority.
	/// </summary>
	public static class Employee
	{
		public const string All = "employee";
		public const string Create = "employee";
		public const string Update = "employee";
		public const string Remove = "employee/{id:Guid}";
	}

	/// <summary>
	/// Contains the public authority.
	/// </summary>
	public static class TemplateMacros
	{
		public const string ByTemplateId = "{id:Guid}";
	}

	/// <summary>
	/// Contains the public authority.
	/// </summary>
	public static class Document
	{
		public const string My = "my";
		public const string MyById = "my/{id:Guid}";
		public const string IsExists = "is-exists";
		public const string File = "file";
		public const string CreateByTemplateId = "create-by-template-id";
		public const string Statistics = "statistics";
		public const string StatisticsDetails = "statistics-details";
		public const string CreateRadarDocument = "create-radar-document";
		public const string CreateToningDocument = "create-toning-document";
		public const string CreateTaxDocument = "create-tax-document";
		public const string CreatePetitionDocument = "create-petition-document";
		public const string CreateTemplateWithMacrosDocument = "create-template-with-macros-document";
	}

	/// <summary>
	/// Contains the template.
	/// </summary>
	public static class Template
	{
		public const string Price = "price";
		public const string AuthorShowingType = "author-showing-type";
		public const string Statistics = "statistics";
		public const string My = "my";
		public const string MyById = "my/{id:Guid}";
	}
}
