/** Standard error response from the API (RFC 7807 ProblemDetails). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Validation errors: field name → list of messages */
  errors?: Record<string, string[]>;
}