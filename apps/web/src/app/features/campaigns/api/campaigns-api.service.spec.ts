import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CampaignsApiService } from './campaigns-api.service';

describe('CampaignsApiService', () => {
  let service: CampaignsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CampaignsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('lists campaigns from the backend contract', async () => {
    const promise = service.listCampaigns();

    const request = http.expectOne('/api/campaigns');
    expect(request.request.method).toBe('GET');
    request.flush([campaignDto({ id: '123e4567-e89b-12d3-a456-426614174000', name: 'Ash Crown' })]);

    await expect(promise).resolves.toEqual([
      campaignDto({ id: '123e4567-e89b-12d3-a456-426614174000', name: 'Ash Crown' }),
    ]);
  });

  it('creates a campaign by name', async () => {
    const promise = service.createCampaign('Ash Crown');

    const request = http.expectOne('/api/campaigns');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Ash Crown' });
    request.flush(campaignDto({ id: '123e4567-e89b-12d3-a456-426614174000', name: 'Ash Crown' }), {
      status: 201,
      statusText: 'Created',
    });

    await expect(promise).resolves.toEqual(
      campaignDto({ id: '123e4567-e89b-12d3-a456-426614174000', name: 'Ash Crown' }),
    );
  });

  it('gets a campaign by encoded id', async () => {
    const promise = service.getCampaign('123e4567-e89b-12d3-a456-426614174000');

    const request = http.expectOne('/api/campaigns/123e4567-e89b-12d3-a456-426614174000');
    expect(request.request.method).toBe('GET');
    request.flush(campaignDto({ id: '123e4567-e89b-12d3-a456-426614174000', name: 'Ash Crown' }));

    await expect(promise).resolves.toEqual(
      campaignDto({ id: '123e4567-e89b-12d3-a456-426614174000', name: 'Ash Crown' }),
    );
  });

  it('maps invalid and duplicate names to field errors', async () => {
    const invalid = service.createCampaign('');
    http
      .expectOne('/api/campaigns')
      .flush(problem('campaign_name_invalid'), { status: 400, statusText: 'Bad Request' });
    await expect(invalid).rejects.toMatchObject({
      kind: 'fieldError',
      code: 'campaign_name_invalid',
      field: 'name',
    });

    const duplicate = service.createCampaign('Ash Crown');
    http
      .expectOne('/api/campaigns')
      .flush(problem('campaign_name_conflict'), { status: 409, statusText: 'Conflict' });
    await expect(duplicate).rejects.toMatchObject({
      kind: 'fieldError',
      code: 'campaign_name_conflict',
      field: 'name',
    });
  });

  it('maps route invalid and not found campaign errors', async () => {
    const invalid = service.getCampaign('not-a-guid');
    http
      .expectOne('/api/campaigns/not-a-guid')
      .flush(problem('campaign_id_invalid'), { status: 400, statusText: 'Bad Request' });
    await expect(invalid).rejects.toMatchObject({
      kind: 'routeInvalid',
      code: 'campaign_id_invalid',
    });

    const missing = service.getCampaign('123e4567-e89b-12d3-a456-426614174000');
    http
      .expectOne('/api/campaigns/123e4567-e89b-12d3-a456-426614174000')
      .flush(problem('campaign_not_found'), { status: 404, statusText: 'Not Found' });
    await expect(missing).rejects.toMatchObject({ kind: 'notFound', code: 'campaign_not_found' });
  });

  it('rejects malformed successful campaign payloads as recoverable errors', async () => {
    const malformedList = service.listCampaigns();
    http.expectOne('/api/campaigns').flush([{ id: 123, name: 'Ash Crown' }]);
    await expect(malformedList).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'unknown_error',
    });

    const malformedItem = service.getCampaign('123e4567-e89b-12d3-a456-426614174000');
    http.expectOne('/api/campaigns/123e4567-e89b-12d3-a456-426614174000').flush({
      id: '123e4567-e89b-12d3-a456-426614174000',
      name: null,
      createdAt: '2026-09-15T10:00:00Z',
      updatedAt: '2026-09-15T10:00:00Z',
    });
    await expect(malformedItem).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'unknown_error',
    });
  });

  it('maps unexpected problem payloads and network errors as recoverable', async () => {
    const unexpected = service.listCampaigns();
    http
      .expectOne('/api/campaigns')
      .flush(problem('unexpected_error'), { status: 500, statusText: 'Internal Server Error' });
    await expect(unexpected).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'unexpected_error',
    });

    const malformed = service.listCampaigns();
    http
      .expectOne('/api/campaigns')
      .flush({ message: 'not problem details' }, { status: 502, statusText: 'Bad Gateway' });
    await expect(malformed).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'unknown_error',
    });

    const network = service.listCampaigns();
    http.expectOne('/api/campaigns').error(new ProgressEvent('error'));
    await expect(network).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'network_error',
    });
  });
});

function campaignDto(overrides: { id: string; name: string }) {
  return {
    id: overrides.id,
    name: overrides.name,
    createdAt: '2026-09-15T10:00:00Z',
    updatedAt: '2026-09-15T10:00:00Z',
  };
}

function problem(code: string) {
  return {
    title: 'Problem',
    status: 400,
    code,
    errors: code.startsWith('campaign_name_') ? { name: [code] } : undefined,
  };
}
