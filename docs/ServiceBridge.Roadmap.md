# ServiceBridge Roadmap

Source: ServiceBridge Project Guideline v1.3.

## PR 1 - Repository and Branding Foundation

Create the clean ServiceBridge solution, project structure, references, foundation primitives, docs, README, and first test. No business behavior change.

## PR 2 - Payment Hardening

Add payment gateway abstraction, callback signature/HMAC validation, PaymentEvent, idempotent callbacks, gateway reference uniqueness, and failed payment recording.

## PR 3 - Catalog v2 Foundation

Add Provider, ServiceVariant, FulfillmentMode, catalog migrations, and Admin query DTOs.

## PR 4 - Variant-based Order Creation

Move order creation to ServiceVariantId, apply pricing scope Variant > Service > Category > Global, and snapshot selected variant data.

## PR 5 - Public Catalog UX

Add category landing pages, service detail, variant selector, price quote, dynamic fields, and checkout.

## PR 6 - Order Tracking UX

Add payment result, my orders, order detail timeline, customer action response, and visible notes.

## PR 7 - Admin Operations v1 + Audit Infrastructure

Add provider/variant/order/payment administration plus AuditLog infrastructure, redaction, and Admin audit log page.

## PR 8 - Ticket Support

Add Ticket and TicketMessage with user, admin, and order-linked support flows.

## PR 9 - In-App Notifications

Add notification storage, unread counts, notification list, and order/ticket event notifications.

## PR 10 - Inventory Foundation

Add InventoryItem, encrypted codes, reserve/deliver flow, and instant-delivery support.

## PR 11 - Reports and Analytics

Add Admin dashboard metrics, revenue/profit reporting, top services, top variants, agent workload, and average completion time.

## PR 12 - SEO and Content

Add SEO fields, FAQ, guide content, category landing content, and service landing content.
