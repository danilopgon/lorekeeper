import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { injectCampaignListQuery, injectCampaignQuery } from './campaign.queries';

const campaignId = '123e4567-e89b-12d3-a456-426614174000';
const campaignDto = {
  id: campaignId,
  name: 'Ash Crown',
  createdAt: '2026-09-15T10:00:00Z',
  updatedAt: '2026-09-15T10:00:00Z',
};

describe('campaign queries', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('lists campaigns with a GET and exposes the validated value', async () => {
    const query = TestBed.runInInjectionContext(() => injectCampaignListQuery());
    TestBed.tick();

    const request = http.expectOne('/api/campaigns');
    expect(request.request.method).toBe('GET');
    request.flush([campaignDto]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(query.value()).toEqual([campaignDto]);
  });

  it('accepts UTC timestamps with a numeric offset as the API serializes them', async () => {
    const query = TestBed.runInInjectionContext(() => injectCampaignListQuery());
    TestBed.tick();

    const dto = {
      ...campaignDto,
      createdAt: '2026-09-15T10:00:00.1234567+00:00',
      updatedAt: '2026-09-15T10:00:00.1234567+00:00',
    };
    http.expectOne('/api/campaigns').flush([dto]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(query.hasValue()).toBe(true);
    expect(query.value()).toEqual([dto]);
  });

  it('surfaces a malformed list payload as an error instead of a value', async () => {
    const query = TestBed.runInInjectionContext(() => injectCampaignListQuery());
    TestBed.tick();

    http.expectOne('/api/campaigns').flush([{ id: 123, name: 'Ash Crown' }]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(query.hasValue()).toBe(false);
    expect(query.error()).toBeTruthy();
  });

  it('gets one campaign by its encoded id', async () => {
    const id = signal<string | undefined>('a/b');
    const query = TestBed.runInInjectionContext(() => injectCampaignQuery(id));
    TestBed.tick();

    const request = http.expectOne('/api/campaigns/a%2Fb');
    expect(request.request.method).toBe('GET');
    request.flush(campaignDto);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(query.value()).toEqual(campaignDto);
  });

  it('sends no request while the campaign id is undefined', () => {
    const id = signal<string | undefined>(undefined);
    TestBed.runInInjectionContext(() => injectCampaignQuery(id));
    TestBed.tick();

    http.expectNone(() => true);
  });

  it('requests the new campaign when the id signal changes', () => {
    const id = signal<string | undefined>(campaignId);
    TestBed.runInInjectionContext(() => injectCampaignQuery(id));
    TestBed.tick();
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaignDto);

    id.set('223e4567-e89b-12d3-a456-426614174000');
    TestBed.tick();

    http.expectOne('/api/campaigns/223e4567-e89b-12d3-a456-426614174000').flush({
      ...campaignDto,
      id: '223e4567-e89b-12d3-a456-426614174000',
    });
  });
});
