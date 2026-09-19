import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CampaignsApiService } from '../../api/campaigns-api.service';
import { Campaign } from '../../models/campaign.model';
import { CampaignSelectionPage } from './campaign-selection.page';

describe('CampaignSelectionPage', () => {
  let fixture: ComponentFixture<CampaignSelectionPage>;
  let campaignsApi: CampaignsApiStub;

  beforeEach(async () => {
    campaignsApi = new CampaignsApiStub();

    await TestBed.configureTestingModule({
      imports: [CampaignSelectionPage],
      providers: [provideRouter([]), { provide: CampaignsApiService, useValue: campaignsApi }],
    }).compileComponents();
  });

  it('renders loading and then empty states without selecting a campaign', async () => {
    campaignsApi.listCampaignsResult = [];
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    expect(text()).toContain('Loading campaigns…');

    await settle();
    expect(text()).toContain('No campaigns yet. Create one to start a chronicle.');
    expect(text()).not.toContain('active campaign');
  });

  it('renders campaigns with explicit Chat and Sources links', async () => {
    campaignsApi.listCampaignsResult = [campaign('123e4567-e89b-12d3-a456-426614174000')];
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    await settle();

    expect(text()).toContain('Ash Crown');
    expect(linkHref('Open Chat')).toBe('/campaigns/123e4567-e89b-12d3-a456-426614174000/chat');
    expect(linkHref('Open Sources')).toBe(
      '/campaigns/123e4567-e89b-12d3-a456-426614174000/sources',
    );
  });

  it('creates a campaign and refreshes the list without navigating implicitly', async () => {
    campaignsApi.listCampaignsResults = [[], [campaign('123e4567-e89b-12d3-a456-426614174000')]];
    campaignsApi.createCampaignResult = campaign('123e4567-e89b-12d3-a456-426614174000');
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    await settle();
    setInputValue('campaign-name', 'Ash Crown');
    submitForm();

    fixture.detectChanges();
    expect(text()).toContain('Creating…');

    await settle();
    expect(campaignsApi.createdNames).toEqual(['Ash Crown']);
    expect(text()).toContain('Ash Crown');
    expect(location.pathname).not.toContain('/campaigns/123e4567-e89b-12d3-a456-426614174000');
  });

  it('renders field-level invalid and conflict errors', async () => {
    campaignsApi.listCampaignsResult = [];
    campaignsApi.createCampaignError = {
      kind: 'fieldError',
      code: 'campaign_name_invalid',
      field: 'name',
    };
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    await settle();
    submitForm();
    await settle();

    expect(text()).toContain('Enter a campaign name between 1 and 120 characters.');

    campaignsApi.createCampaignError = {
      kind: 'fieldError',
      code: 'campaign_name_conflict',
      field: 'name',
    };
    submitForm();
    await settle();

    expect(text()).toContain('A campaign with this name already exists.');
  });

  it('renders recoverable list and creation errors with retry', async () => {
    campaignsApi.listCampaignsError = { kind: 'recoverableError', code: 'network_error' };
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    await settle();

    expect(text()).toContain('Campaigns could not be loaded. Retry when the API is available.');
    expect(button('Retry')).toBeTruthy();

    campaignsApi.createCampaignError = { kind: 'recoverableError', code: 'network_error' };
    setInputValue('campaign-name', 'Ash Crown');
    submitForm();
    await settle();

    expect(text()).toContain('Campaign creation failed. Check the connection and retry.');
  });

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function linkHref(label: string): string | null {
    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a'));
    return links.find((link) => link.textContent?.trim() === label)?.getAttribute('href') ?? null;
  }

  function button(label: string): HTMLButtonElement | undefined {
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
    return buttons.find((candidate) => candidate.textContent?.trim() === label);
  }

  function setInputValue(id: string, value: string): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submitForm(): void {
    const form = (fixture.nativeElement as HTMLElement).querySelector('form')!;
    form.dispatchEvent(new Event('submit'));
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }
});

class CampaignsApiStub {
  listCampaignsResult: Campaign[] = [];
  listCampaignsResults: Campaign[][] = [];
  listCampaignsError: unknown;
  createCampaignResult = campaign('123e4567-e89b-12d3-a456-426614174000');
  createCampaignError: unknown;
  readonly createdNames: string[] = [];

  async listCampaigns(): Promise<Campaign[]> {
    if (this.listCampaignsError) {
      throw this.listCampaignsError;
    }

    return this.listCampaignsResults.shift() ?? this.listCampaignsResult;
  }

  async createCampaign(name: string): Promise<Campaign> {
    this.createdNames.push(name);

    if (this.createCampaignError) {
      throw this.createCampaignError;
    }

    return this.createCampaignResult;
  }
}

function campaign(id: string): Campaign {
  return {
    id,
    name: 'Ash Crown',
    createdAt: '2026-09-15T10:00:00Z',
    updatedAt: '2026-09-15T10:00:00Z',
  };
}
