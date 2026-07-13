namespace M1.Domain;

public enum UserRole { Customer = 0, Admin = 1 }

public enum EmailTokenPurpose { VerifyEmail = 0, ResetPassword = 1 }

public enum CouponType { Percent = 0, Fixed = 1 }

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum PaymentProvider { Fake = 0, Mpesa = 1, Stripe = 2 }

public enum PaymentStatus { Pending = 0, Succeeded = 1, Failed = 2, Refunded = 3 }

public enum NotificationType { OrderUpdate = 0, Promotion = 1, System = 2 }
