import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CampaignWorkspaceShellComponent } from './campaign-workspace-shell.component';

const firstId = '123e4567-e89b-12d3-a456-426614174000';
const secondId = '223e4567-e89b-12d3-a456-426614174000';

describe('CampaignWorkspaceShellComponent', () => {
  let fixture: ComponentFixture<CampaignWorkspaceShellComponent>;
  let http: HttpTestingController;
  let route: ActivatedRouteStub;

  beforeEach(async () => {
    route = new ActivatedRouteStub(firstId);

    await TestBed.configureTestingModule({
      imports: [CampaignWorkspaceShellComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: route },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('loads valid campaign context for the Chat shell without exposing unavailable controls', async () => {
    createShell('chat');
    expect(text()).toContain('Cargando campaña…');

    http.expectOne(`/api/campaigns/${firstId}`).flush(campaign(firstId));
    await settle();

    expect(heading()).toBe('Ash Crown');
    expect(text()).toContain('El chat aún no está disponible');
    expect(currentSection()).toBe('Chat');
    expect(sectionHref('Fuentes')).toBe(`/campaigns/${firstId}/sources`);
    expect(text()).not.toContain(firstId);
    expect(prohibitedControls()).toEqual([]);
  });

  it('loads valid campaign context for the Sources shell without exposing ingestion controls', async () => {
    createShell('sources');
    http.expectOne(`/api/campaigns/${firstId}`).flush(campaign(firstId));
    await settle();

    expect(heading()).toBe('Ash Crown');
    expect(text()).toContain('Las fuentes aún no están disponibles');
    expect(currentSection()).toBe('Fuentes');
    expect(prohibitedControls()).toEqual([]);
  });

  it('renders malformed campaign route state before calling the API', async () => {
    route.setCampaignId('not-a-campaign-id');
    createShell('chat');
    await settle();

    http.expectNone(() => true);
    expect(heading()).toBe('Enlace de campaña no válido');
    expect(text()).toContain('Este enlace no corresponde a ninguna campaña.');
  });

  it('renders unknown campaign and recoverable API states', async () => {
    createShell('chat');
    http
      .expectOne(`/api/campaigns/${firstId}`)
      .flush({ code: 'campaign_not_found' }, { status: 404, statusText: 'Not Found' });
    await settle();

    expect(heading()).toBe('Campaña no encontrada');

    route.setCampaignId(secondId);
    await settle();
    http.expectOne(`/api/campaigns/${secondId}`).error(new ProgressEvent('error'));
    await settle();

    expect(heading()).toBe('No se pudo cargar la campaña');
    expect(button('Reintentar')).toBeTruthy();
  });

  it('renders a schema-invalid campaign as the recoverable state', async () => {
    createShell('chat');
    http.expectOne(`/api/campaigns/${firstId}`).flush({ ...campaign(firstId), name: null });
    await settle();

    expect(heading()).toBe('No se pudo cargar la campaña');
  });

  it('does not display the previous campaign while a new campaign id is resolving', async () => {
    createShell('chat');
    http.expectOne(`/api/campaigns/${firstId}`).flush(campaign(firstId));
    await settle();
    expect(text()).toContain('Ash Crown');

    route.setCampaignId(secondId);
    fixture.detectChanges();

    expect(text()).toContain('Cargando campaña…');
    expect(text()).not.toContain('Ash Crown');

    await settle();
    http.expectOne(`/api/campaigns/${secondId}`).flush(campaign(secondId));
  });

  function createShell(kind: 'chat' | 'sources'): void {
    fixture = TestBed.createComponent(CampaignWorkspaceShellComponent);
    fixture.componentRef.setInput('workspaceKind', kind);
    fixture.detectChanges();
    TestBed.tick();
  }

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function heading(): string | undefined {
    return (fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent?.trim();
  }

  function currentSection(): string | undefined {
    return (fixture.nativeElement as HTMLElement)
      .querySelector('a[aria-current="page"]')
      ?.textContent?.trim();
  }

  function sectionHref(label: string): string | null {
    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('nav a'));
    return links.find((link) => link.textContent?.trim() === label)?.getAttribute('href') ?? null;
  }

  function button(label: string): HTMLButtonElement | undefined {
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
    return buttons.find((candidate) => candidate.textContent?.trim() === label);
  }

  function prohibitedControls(): string[] {
    return Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll(
        'input, textarea, select, button[type="submit"], [aria-label*="citation" i], [aria-label*="upload" i]',
      ),
    ).map((element) => element.textContent?.trim() ?? element.tagName);
  }

  async function settle(): Promise<void> {
    for (let turn = 0; turn < 3; turn += 1) {
      await new Promise((resolve) => setTimeout(resolve));
      TestBed.tick();
      fixture.detectChanges();
    }
  }
});

class ActivatedRouteStub {
  private readonly paramMapSubject: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  readonly paramMap;
  readonly snapshot;

  constructor(campaignId: string) {
    const initial = convertToParamMap({ campaignId });
    this.paramMapSubject = new BehaviorSubject(initial);
    this.paramMap = this.paramMapSubject.asObservable();
    this.snapshot = { paramMap: initial };
  }

  setCampaignId(campaignId: string): void {
    const next = convertToParamMap({ campaignId });
    this.snapshot.paramMap = next;
    this.paramMapSubject.next(next);
  }
}

function campaign(id: string) {
  return {
    id,
    name: id.startsWith('123') ? 'Ash Crown' : 'Bright Vale',
    createdAt: '2026-09-15T10:00:00Z',
    updatedAt: '2026-09-15T10:00:00Z',
  };
}
