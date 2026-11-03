import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CampaignSelectionPage } from './campaign-selection.page';

const campaignId = '123e4567-e89b-12d3-a456-426614174000';
const listRequest = { method: 'GET', url: '/api/campaigns' };
const createRequest = { method: 'POST', url: '/api/campaigns' };

describe('CampaignSelectionPage', () => {
  let fixture: ComponentFixture<CampaignSelectionPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignSelectionPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('renders loading and then empty states without selecting a campaign', async () => {
    createPage();
    expect(text()).toContain('Cargando campañas…');

    http.expectOne(listRequest).flush([]);
    await settle();

    expect(text()).toContain('Aún no hay campañas. Crea una para empezar su crónica.');
    expect(folio()).toBeNull();
  });

  it('renders campaigns with explicit Chat and Fuentes links, a count folio and Spanish dates', async () => {
    createPage();
    http.expectOne(listRequest).flush([campaign(campaignId)]);
    await settle();

    expect(text()).toContain('Ash Crown');
    expect(linkHref('Chat')).toBe(`/campaigns/${campaignId}/chat`);
    expect(linkHref('Fuentes')).toBe(`/campaigns/${campaignId}/sources`);
    expect(folio()).toBe('1 campaña');
    expect(text()).toContain('15 sept 2026');
    expect(text()).not.toContain('2026-09-15T10:00:00Z');
  });

  it('creates a campaign and refreshes the list without navigating implicitly', async () => {
    createPage();
    http.expectOne(listRequest).flush([]);
    await settle();

    setInputValue('campaign-name', 'Ash Crown');
    submitForm();
    fixture.detectChanges();
    expect(text()).toContain('Creando…');

    const create = http.expectOne(createRequest);
    expect(create.request.body).toEqual({ name: 'Ash Crown' });
    create.flush(campaign(campaignId), { status: 201, statusText: 'Created' });
    await settle();

    http.expectOne(listRequest).flush([campaign(campaignId)]);
    await settle();

    expect(text()).toContain('Ash Crown');
    expect(text()).not.toContain('Creando…');
    expect(location.pathname).not.toContain(`/campaigns/${campaignId}`);
  });

  it('reloads the list when a campaign is created during an active load', async () => {
    createPage();
    setInputValue('campaign-name', 'Ash Crown');
    submitForm();

    const list = http.expectOne(listRequest);
    http
      .expectOne(createRequest)
      .flush(campaign(campaignId), { status: 201, statusText: 'Created' });
    await settle();
    list.flush([]);
    await settle();

    http.expectOne(listRequest).flush([campaign(campaignId)]);
    await settle();

    expect(text()).toContain('Ash Crown');
  });

  it('renders field-level invalid and conflict errors', async () => {
    createPage();
    http.expectOne(listRequest).flush([]);
    await settle();

    submitForm();
    http
      .expectOne(createRequest)
      .flush(
        { code: 'campaign_name_invalid', errors: { name: ['campaign_name_invalid'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await settle();

    expect(text()).toContain('Escribe un nombre de entre 1 y 120 caracteres.');
    expect(nameInput().getAttribute('aria-invalid')).toBe('true');
    expect(nameInput().getAttribute('aria-describedby')).toBe('campaign-name-error');

    submitForm();
    http
      .expectOne(createRequest)
      .flush(
        { code: 'campaign_name_conflict', errors: { name: ['campaign_name_conflict'] } },
        { status: 409, statusText: 'Conflict' },
      );
    await settle();

    expect(text()).toContain('Ya existe una campaña con ese nombre.');
  });

  it('renders recoverable list and creation errors with retry', async () => {
    createPage();
    http.expectOne(listRequest).error(new ProgressEvent('error'));
    await settle();

    expect(text()).toContain(
      'No se pudieron cargar las campañas. Vuelve a intentarlo cuando el servidor responda.',
    );

    button('Reintentar')!.click();
    await settle();
    http.expectOne(listRequest).flush([]);
    await settle();
    expect(text()).toContain('Aún no hay campañas. Crea una para empezar su crónica.');

    setInputValue('campaign-name', 'Ash Crown');
    submitForm();
    http.expectOne(createRequest).error(new ProgressEvent('error'));
    await settle();

    expect(text()).toContain(
      'No se pudo crear la campaña. Comprueba la conexión y vuelve a intentarlo.',
    );
  });

  function createPage(): void {
    fixture = TestBed.createComponent(CampaignSelectionPage);
    fixture.detectChanges();
    TestBed.tick();
  }

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function folio(): string | null {
    return (
      (fixture.nativeElement as HTMLElement).querySelector('p.folio')?.textContent?.trim() ?? null
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
    for (let turn = 0; turn < 3; turn += 1) {
      await new Promise((resolve) => setTimeout(resolve));
      TestBed.tick();
      fixture.detectChanges();
    }
  }
});

function campaign(id: string) {
  return {
    id,
    name: 'Ash Crown',
    createdAt: '2026-09-15T10:00:00Z',
    updatedAt: '2026-09-15T10:00:00Z',
  };
}
