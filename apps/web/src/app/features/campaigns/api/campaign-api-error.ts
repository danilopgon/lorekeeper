import { HttpErrorResponse } from '@angular/common/http';
import { zProblemDetails } from '../../../shared/api/generated/zod.gen';

/** Typed UI-facing error states produced by the campaign API layer. */
export type CampaignApiError =
  | {
      kind: 'fieldError';
      code: 'campaign_name_invalid' | 'campaign_name_conflict';
      field: 'name';
    }
  | { kind: 'routeInvalid'; code: 'campaign_id_invalid' }
  | { kind: 'notFound'; code: 'campaign_not_found' }
  | {
      kind: 'recoverableError';
      code: 'unexpected_error' | 'network_error' | 'unknown_error';
    };

const unknownError: CampaignApiError = { kind: 'recoverableError', code: 'unknown_error' };

/** Maps transport, validation and resource errors into UI-facing campaign errors. */
export function toCampaignApiError(error: unknown): CampaignApiError {
  const source = unwrapCause(error);

  if (isCampaignApiError(source)) {
    return source;
  }

  if (source instanceof HttpErrorResponse) {
    return source.status === 0
      ? { kind: 'recoverableError', code: 'network_error' }
      : mapProblemBody(source.error);
  }

  return unknownError;
}

function mapProblemBody(body: unknown): CampaignApiError {
  const problem = zProblemDetails.safeParse(body);

  switch (problem.data?.code) {
    case 'campaign_name_invalid':
    case 'campaign_name_conflict':
      return { kind: 'fieldError', code: problem.data.code, field: 'name' };
    case 'campaign_id_invalid':
      return { kind: 'routeInvalid', code: problem.data.code };
    case 'campaign_not_found':
      return { kind: 'notFound', code: problem.data.code };
    case 'unexpected_error':
      return { kind: 'recoverableError', code: problem.data.code };
    default:
      return unknownError;
  }
}

function unwrapCause(error: unknown): unknown {
  let current = error;

  while (current instanceof Error && !(current instanceof HttpErrorResponse) && current.cause) {
    current = current.cause;
  }

  return current;
}

function isCampaignApiError(value: unknown): value is CampaignApiError {
  return typeof value === 'object' && value !== null && 'kind' in value && 'code' in value;
}
