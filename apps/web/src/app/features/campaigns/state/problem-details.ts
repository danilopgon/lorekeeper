/** Stable Problem Details codes returned by the campaign HTTP API. */
export type CampaignProblemCode =
  | 'campaign_name_invalid'
  | 'campaign_name_conflict'
  | 'campaign_id_invalid'
  | 'campaign_not_found'
  | 'unexpected_error';

/** RFC 7807 payload shape with Lorekeeper campaign extensions. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

/** Typed UI-facing error states produced by the campaign API adapter. */
export type CampaignApiError =
  | {
      kind: 'fieldError';
      code: 'campaign_name_invalid' | 'campaign_name_conflict';
      field: 'name';
      details?: ProblemDetails;
    }
  | { kind: 'routeInvalid'; code: 'campaign_id_invalid'; details?: ProblemDetails }
  | { kind: 'notFound'; code: 'campaign_not_found'; details?: ProblemDetails }
  | {
      kind: 'recoverableError';
      code: 'unexpected_error' | 'network_error' | 'unknown_error';
      details?: ProblemDetails;
    };

/** Maps RFC 7807 campaign codes into UI-facing adapter errors. */
export function mapProblemDetails(details: ProblemDetails | null | undefined): CampaignApiError {
  switch (details?.code) {
    case 'campaign_name_invalid':
    case 'campaign_name_conflict':
      return { kind: 'fieldError', code: details.code, field: 'name', details };
    case 'campaign_id_invalid':
      return { kind: 'routeInvalid', code: details.code, details };
    case 'campaign_not_found':
      return { kind: 'notFound', code: details.code, details };
    case 'unexpected_error':
      return { kind: 'recoverableError', code: details.code, details };
    default:
      return { kind: 'recoverableError', code: 'unknown_error', details: details ?? undefined };
  }
}
