import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CampaignCommands } from './campaign-commands.service';

const campaignDto = {
  id: '123e4567-e89b-12d3-a456-426614174000',
  name: 'Ash Crown',
  createdAt: '2026-09-15T10:00:00Z',
  updatedAt: '2026-09-15T10:00:00Z',
};

describe('CampaignCommands', () => {
  let commands: CampaignCommands;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    commands = TestBed.inject(CampaignCommands);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('creates a campaign with a POST and returns the validated campaign', async () => {
    const created = commands.createCampaign({ name: 'Ash Crown' });

    const request = http.expectOne('/api/campaigns');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Ash Crown' });
    request.flush(campaignDto, { status: 201, statusText: 'Created' });

    await expect(created).resolves.toEqual(campaignDto);
  });

  it.each([
    ['campaign_name_invalid', 400, 'Bad Request'],
    ['campaign_name_conflict', 409, 'Conflict'],
  ] as const)('maps %s to a name field error', async (code, status, statusText) => {
    const created = commands.createCampaign({ name: 'Ash Crown' });

    http
      .expectOne('/api/campaigns')
      .flush({ title: 'Problem', status, code, errors: { name: [code] } }, { status, statusText });

    await expect(created).rejects.toMatchObject({ kind: 'fieldError', code, field: 'name' });
  });

  it('rejects a malformed created campaign as a recoverable error', async () => {
    const created = commands.createCampaign({ name: 'Ash Crown' });

    http.expectOne('/api/campaigns').flush({ ...campaignDto, name: null });

    await expect(created).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'unknown_error',
    });
  });

  it('maps a network failure to a recoverable error', async () => {
    const created = commands.createCampaign({ name: 'Ash Crown' });

    http.expectOne('/api/campaigns').error(new ProgressEvent('error'));

    await expect(created).rejects.toMatchObject({
      kind: 'recoverableError',
      code: 'network_error',
    });
  });
});
