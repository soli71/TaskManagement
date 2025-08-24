using Microsoft.AspNetCore.Identity;

namespace TaskManagementMvc.Services;

public class PersianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new IdentityError { Code = nameof(DefaultError), Description = "خطای نامشخص رخ داد." };
    public override IdentityError ConcurrencyFailure() => new IdentityError { Code = nameof(ConcurrencyFailure), Description = "خطای همزمانی رخ داد. لطفاً دوباره تلاش کنید." };
    public override IdentityError PasswordMismatch() => new IdentityError { Code = nameof(PasswordMismatch), Description = "رمز عبور نادرست است." };
    public override IdentityError InvalidToken() => new IdentityError { Code = nameof(InvalidToken), Description = "توکن نامعتبر است." };
    public override IdentityError LoginAlreadyAssociated() => new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = "این ورود قبلاً به یک حساب دیگر متصل شده است." };
    public override IdentityError InvalidUserName(string? userName) => new IdentityError { Code = nameof(InvalidUserName), Description = $"نام کاربری '{userName}' نامعتبر است." };
    public override IdentityError InvalidEmail(string? email) => new IdentityError { Code = nameof(InvalidEmail), Description = $"ایمیل '{email}' نامعتبر است." };
    public override IdentityError DuplicateUserName(string userName) => new IdentityError { Code = nameof(DuplicateUserName), Description = $"نام کاربری '{userName}' قبلاً گرفته شده است." };
    public override IdentityError DuplicateEmail(string email) => new IdentityError { Code = nameof(DuplicateEmail), Description = $"ایمیل '{email}' قبلاً ثبت شده است." };
    public override IdentityError InvalidRoleName(string? role) => new IdentityError { Code = nameof(InvalidRoleName), Description = $"نام نقش '{role}' نامعتبر است." };
    public override IdentityError DuplicateRoleName(string role) => new IdentityError { Code = nameof(DuplicateRoleName), Description = $"نام نقش '{role}' قبلاً ثبت شده است." };
    public override IdentityError UserAlreadyHasPassword() => new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "برای کاربر رمز عبور از قبل تنظیم شده است." };
    public override IdentityError UserLockoutNotEnabled() => new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "قفل کردن حساب برای این کاربر فعال نیست." };
    public override IdentityError UserAlreadyInRole(string role) => new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"کاربر از قبل نقش '{role}' را دارد." };
    public override IdentityError UserNotInRole(string role) => new IdentityError { Code = nameof(UserNotInRole), Description = $"کاربر نقش '{role}' را ندارد." };
    public override IdentityError PasswordTooShort(int length) => new IdentityError { Code = nameof(PasswordTooShort), Description = $"رمز عبور باید حداقل {length} کاراکتر باشد." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "رمز عبور باید شامل حداقل یک کاراکتر غیر حرفی-عددی باشد." };
    public override IdentityError PasswordRequiresDigit() => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "رمز عبور باید شامل حداقل یک رقم باشد." };
    public override IdentityError PasswordRequiresLower() => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "رمز عبور باید شامل حداقل یک حرف کوچک باشد." };
    public override IdentityError PasswordRequiresUpper() => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "رمز عبور باید شامل حداقل یک حرف بزرگ باشد." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"رمز عبور باید حداقل شامل {uniqueChars} کاراکتر یکتا باشد." };
}
