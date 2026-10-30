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
    expect(text()).toContain('Cargando campañas…');

    await settle();
    expect(text()).toContain('Aún no hay campañas. Crea una para empezar su crónica.');
    expect(folio()).toBeNull();
  });

  it('renders campaigns with explicit Chat and Fuentes links, a count folio and Spanish dates', async () => {
    campaignsApi.listCampaignsResult = [campaign('123e4567-e89b-12d3-a456-426614174000')];
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    await settle();

    expect(text()).toContain('Ash Crown');
    expect(linkHref('Chat')).toBe('/campaigns/123e4567-e89b-12d3-a456-426614174000/chat');
    expect(linkHref('Fuentes')).toBe('/campaigns/123e4567-e89b-12d3-a456-426614174000/sources');
    expect(folio()).toBe('1 campaña');
    expect(text()).toContain('15 sept 2026');
    expect(text()).not.toContain('2026-09-15T10:00:00Z');
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
    expect(text()).toContain('Creando…');

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

    expect(text()).toContain('Escribe un nombre de entre 1 y 120 caracteres.');
    expect(nameInput().getAttribute('aria-invalid')).toBe('true');
    expect(nameInput().getAttribute('aria-describedby')).toBe('campaign-name-error');

    campaignsApi.createCampaignError = {
      kind: 'fieldError',
      code: 'campaign_name_conflict',
      field: 'name',
    };
    submitForm();
    await settle();

    expect(text()).toContain('Ya existe una campaña con ese nombre.');
  });

  it('renders recoverable list and creation errors with retry', async () => {
    campaignsApi.listCampaignsError = { kind: 'recoverableError', code: 'network_error' };
    fixture = TestBed.createComponent(CampaignSelectionPage);

    fixture.detectChanges();
    await settle();

    expect(text()).toContain(
      'No se pudieron cargar las campañas. Vuelve a intentarlo cuando el servidor responda.',
    );
    expect(button('Reintentar')).toBeTruthy();

    campaignsApi.createCampaignError = { kind: 'recoverableError', code: 'network_error' };
    setInputValue('campaign-name', 'Ash Crown');
    submitForm();
    await settle();

    expect(text()).toContain(
      'No se pudo crear la campaña. Comprueba la conexión y vuelve a intentarlo.',
    );
  });

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function folio(): string | null {
    return (
      (fixture.nativeElement as HTMLElement)
        .querySelector('header > p.folio')
        ?.textContent?.trim() ?? null
    );
  }

  function nameInput(): HTMLInputElement {
    return (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      '#campaign-name',
    )!;
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
