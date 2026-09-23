import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/problem-details';

/** Turns any error from an API call into a message we can show the user. */
export function getErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    // Status 0 = the request never reached the server (API not running, no network)
    if (error.status === 0) {
      return 'Cannot reach the server. Please check your connection and try again.';
    }

    const problem = error.error as ProblemDetails | null;

    // Validation errors: { errors: { Email: ["..."], Password: ["..."] } }
    if (problem?.errors) {
      const messages = Object.values(problem.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (problem?.detail) {
      return problem.detail;
    }

    if (problem?.title) {
      return problem.title;
    }
  }

  return 'Something went wrong. Please try again.';
}