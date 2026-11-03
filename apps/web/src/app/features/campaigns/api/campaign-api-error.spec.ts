import { HttpErrorResponse } from '@angular/common/http';
import { z } from 'zod';
import { toCampaignApiError } from './campaign-api-error';

function httpError(status: number, body: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body });
}

describe('toCampaignApiError', () => {
  it.each([
    ['campaign_name_invalid', 'fieldError'],
    ['campaign_name_conflict', 'fieldError'],
    ['campaign_id_invalid', 'routeInvalid'],
    ['campaign_not_found', 'notFound'],
    ['unexpected_error', 'recoverableError'],
  ])('maps the problem code %s to %s', (code, kind) => {
    expect(toCampaignApiError(httpError(400, { title: 'Problem', code }))).toMatchObject({
      kind,
      code,
    });
  });

  it('adds the name field to field errors', () => {
    const problem = { code: 'campaign_name_invalid', errors: { name: ['campaign_name_invalid'] } };

    expect(toCampaignApiError(httpError(400, problem))).toMatchObject({ field: 'name' });
  });

  it.each([
    ['a problem without a code', { title: 'Bad Request' }],
    ['an unknown code', { code: 'something_else' }],
    ['a body that is not a problem', 'Bad Gateway'],
    ['a problem with an invalid code type', { code: 42 }],
    ['an empty body', null],
  ])('maps %s to an unknown recoverable error', (_label, body) => {
    expect(toCampaignApiError(httpError(502, body))).toMatchObject({
      kind: 'recoverableError',
      code: 'unknown_error',
    });
  });

  it('maps status 0 to a network error', () => {
    expect(toCampaignApiError(httpError(0, new ProgressEvent('error')))).toEqual({
      kind: 'recoverableError',
      code: 'network_error',
    });
  });

  it('maps a validation failure to an unknown recoverable error', () => {
    const failure = z.string().safeParse(1).error;

    expect(toCampaignApiError(failure)).toEqual({
      kind: 'recoverableError',
      code: 'unknown_error',
    });
  });

  it('unwraps errors that a resource wrapped in a cause', () => {
    const wrapped = new Error('Resource failed', {
      cause: httpError(404, { code: 'campaign_not_found' }),
    });

    expect(toCampaignApiError(wrapped)).toMatchObject({
      kind: 'notFound',
      code: 'campaign_not_found',
    });
  });

  it('returns an already mapped error unchanged', () => {
    const mapped = { kind: 'notFound', code: 'campaign_not_found' } as const;

    expect(toCampaignApiError(mapped)).toBe(mapped);
  });
});
