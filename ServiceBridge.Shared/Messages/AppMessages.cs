namespace ServiceBridge.Shared.Messages;

public static class AppMessages
{
    public const string RegistrationSuccess = "Registration successful. Please login.";
    public const string LoginSuccess = "Login successful.";
    public const string DuplicateEmail = "A user with this email already exists.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string UserInactive = "Your account has been deactivated. Please contact support.";
    public const string UserNotFound = "User not found.";
    public const string ValidationFailed = "Validation failed.";
    public const string UnexpectedError = "An unexpected error occurred.";
    public const string SessionExpired = "Your session has expired. Please sign in again.";
    public const string ProfileRetrieved = "Profile retrieved successfully.";
    public const string UnauthorizedAccess = "You are not authorized to perform this action.";
    public const string CategoryNotFound = "Service category not found.";
    public const string ServiceNotFound = "Digital service not found.";
    public const string ServiceVariantNotFound = "Service variant not found.";
    public const string RequiredFieldNotFound = "Service required field not found.";
    public const string DuplicateCategorySlug = "A category with this slug already exists.";
    public const string DuplicateServiceSlug = "A service with this slug already exists.";
    public const string DuplicateVariantSlug = "A variant with this slug already exists for this service.";
    public const string DuplicateFieldKey = "A field with this key already exists for this service.";
    public const string CategoryCreated = "Service category created successfully.";
    public const string CategoryUpdated = "Service category updated successfully.";
    public const string ServiceCreated = "Digital service created successfully.";
    public const string ServiceUpdated = "Digital service updated successfully.";
    public const string ServiceVariantCreated = "Service variant created successfully.";
    public const string ServiceVariantUpdated = "Service variant updated successfully.";
    public const string RequiredFieldCreated = "Service required field created successfully.";
    public const string RequiredFieldUpdated = "Service required field updated successfully.";
    public const string ExchangeRateNotFound = "No active exchange rate is available for this service.";
    public const string ProfitMarginNotFound = "No active profit margin is available for this service.";
    public const string InvalidPricingDate = "Pricing date must be specified in UTC.";
    public const string PricingUnavailable = "Pricing is currently unavailable for this service.";
    public const string ExchangeRateCreated = "Exchange rate created successfully.";
    public const string ProfitMarginCreated = "Profit margin created successfully.";
    public const string ExchangeRateRecordNotFound = "Exchange rate record not found.";
    public const string ProfitMarginRecordNotFound = "Profit margin record not found.";
    public const string ExchangeRateStatusUpdated = "Exchange rate status updated successfully.";
    public const string ProfitMarginStatusUpdated = "Profit margin status updated successfully.";
    public const string InvalidProfitMarginScope =
        "Specify a service variant, service or category, not more than one.";
    public const string OrderCreated = "Order created successfully.";
    public const string OrderNumberGenerationFailed = "Unable to generate a unique order number.";
    public const string FulfillmentPolicyRequired =
        "A fulfillment policy is required before this service variant can be purchased.";
    public const string OrderNotFound = "Order not found.";
    public const string OrderCancelled = "Order cancelled successfully.";
    public const string OrderCancelledByUser = "Cancelled by customer.";
    public const string OrderCannotBeCancelled = "This order can no longer be cancelled.";
    public const string OrderChangedDuringCancellation =
        "The order changed while it was being cancelled. Please refresh and try again.";
    public const string PaymentInitiated = "Payment initiated successfully.";
    public const string PaymentConfirmed = "Payment confirmed successfully.";
    public const string PaymentFailed = "Payment failure recorded.";
    public const string PaymentRefunded = "Payment refund recorded.";
    public const string PaymentNotFound = "Payment not found.";
    public const string PaymentGatewayDisabled =
        "The selected payment gateway is currently disabled.";
    public const string PaymentGatewayUnavailable =
        "The selected payment gateway is currently unavailable.";
    public const string PaymentGatewayUnsupported =
        "The selected payment gateway is not supported.";
    public const string PaymentGatewayVerificationFailed =
        "Payment gateway verification failed.";
    public const string PaymentOrderForbidden =
        "You are not allowed to initiate payment for this order.";
    public const string PaymentOrderNotPending =
        "This order is not waiting for payment.";
    public const string PendingPaymentExists =
        "A pending payment already exists for this order.";
    public const string PaymentAlreadyProcessed =
        "This payment has already been processed.";
    public const string OrderPaidByConfirmedPayment =
        "Payment confirmed by the payment callback.";
    public const string RefundRequestCreated = "Refund request submitted successfully.";
    public const string RefundRequestNotFound = "Refund request not found.";
    public const string RefundRequestApproved = "Refund request approved.";
    public const string RefundRequestRejected = "Refund request rejected.";
    public const string RefundOrderStatusNotEligible =
        "This order is not eligible for a refund request.";
    public const string ActiveRefundRequestExists =
        "An active refund request already exists for this payment.";
    public const string InvalidRefundTransition =
        "This refund request cannot move to the requested status.";
    public const string RefundChangedDuringUpdate =
        "The refund request changed while it was being updated. Please refresh and try again.";
    public const string AgentNotFound = "Active agent not found.";
    public const string OrderAssigned = "Order assigned successfully.";
    public const string OrderAssignedToAgent = "Order assigned to an agent.";
    public const string OrderCannotBeAssigned =
        "Only paid, unassigned orders can be assigned.";
    public const string OrderChangedDuringAssignment =
        "The order changed while it was being assigned. Please refresh and try again.";
    public const string OrderProcessingStarted = "Order processing started.";
    public const string CustomerActionRequested =
        "Customer action was requested.";
    public const string OrderProcessingResumed = "Order processing resumed.";
    public const string OrderCompleted = "Order completed successfully.";
    public const string DeliveryCredentialsSaved =
        "Delivery credentials saved successfully.";
    public const string RequiredDeliveryCredentialsMissing =
        "Required delivery credentials must be saved before completing this order.";
    public const string OrderFailed = "Order marked as failed.";
    public const string OrderNoteAdded = "Order note added successfully.";
    public const string InvalidAgentOrderOperation =
        "This operation is not allowed for the current order state.";
    public const string OrderChangedDuringAgentProcessing =
        "The order changed while it was being processed. Please refresh and try again.";
    public const string CustomerActionResponded =
        "Customer response submitted successfully.";
    public const string CustomerActionResponseRejected =
        "This customer action request cannot be answered.";
    public const string OrderChangedDuringCustomerAction =
        "The order changed while the response was being submitted. Please refresh and try again.";
    public const string InvalidPasswordPolicy =
        "Password must be between 6 and 100 characters.";
    public const string CurrentPasswordIncorrect =
        "Current password is incorrect.";
    public const string PasswordResetSuccessfully =
        "Password reset successfully.";
    public const string PasswordChangedSuccessfully =
        "Password changed successfully.";
    public const string AgentCreated = "Agent created successfully.";
    public const string UserStatusUpdated = "User status updated successfully.";
    public const string AdminStatusCannotBeChanged =
        "Admin account status cannot be changed from this endpoint.";
    public const string InvalidInventoryImportRequest =
        "Inventory import request is invalid.";
    public const string InventoryRequiresInventoryCodeFulfillment =
        "The selected service variant does not support inventory.";
    public const string InventoryVariantInactive =
        "Inventory cannot be added to an inactive service variant.";
    public const string InventoryProtectionUnavailable =
        "Inventory protection is currently unavailable.";
    public const string InventoryDuplicateValue =
        "An inventory item with a supplied value already exists.";
    public const string InventoryOperationUnavailable =
        "The inventory operation could not be completed.";
    public const string InventoryItemsCreated = "Inventory items created successfully.";
    public const string InvalidInventoryDisableRequest =
        "Inventory disable request is invalid.";
    public const string InventoryItemNotFound = "Inventory item not found.";
    public const string InventoryItemCannotBeDisabled =
        "This inventory item cannot be disabled in its current state.";
    public const string InventoryItemChangedDuringUpdate =
        "The inventory item changed while it was being updated. Please refresh and try again.";
    public const string InventoryItemDisabled = "Inventory item disabled successfully.";
    public const string InvalidInventoryPage = "Inventory paging parameters are invalid.";
}
