import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CampaignsApiService } from '../../api/campaigns-api.service';
import { Campaign } from '../../models/campaign.model';
import { CampaignWorkspaceShellComponent } from './campaign-workspace-shell.component';

describe('CampaignWorkspaceShellComponent', () => {
  let fixture: ComponentFixture<CampaignWorkspaceShellComponent>;
  let campaignsApi: CampaignsApiStub;
  let route: ActivatedRouteStub;

  beforeEach(async () => {
    campaignsApi = new CampaignsApiStub();
    route = new ActivatedRouteStub('123e4567-e89b-12d3-a456-426614174000');

    await TestBed.configureTestingModule({
      imports: [CampaignWorkspaceShellComponent],
      providers: [
        provideRouter([]),
        { provide: CampaignsApiService, useValue: campaignsApi },
        { provide: ActivatedRoute, useValue: route },
      ],
    }).compileComponents();
  });

  it('loads valid campaign context for the Chat shell without exposing unavailable controls', async () => {
    campaignsApi.campaigns.set(
      '123e4567-e89b-12d3-a456-426614174000',
      campaign('123e4567-e89b-12d3-a456-426614174000'),
    );
    createShell('chat');

    fixture.detectChanges();
    expect(text()).toContain('Cargando campaña…');

    await settle();

    expect(campaignsApi.requestedIds).toEqual(['123e4567-e89b-12d3-a456-426614174000']);
    expect(heading()).toBe('Ash Crown');
    expect(text()).toContain('El chat aún no está disponible');
    expect(currentSection()).toBe('Chat');
    expect(sectionHref('Fuentes')).toBe('/campaigns/123e4567-e89b-12d3-a456-426614174000/sources');
    expect(text()).not.toContain('123e4567-e89b-12d3-a456-426614174000');
    expect(prohibitedControls()).toEqual([]);
  });

  it('loads valid campaign context for the Sources shell without exposing ingestion controls', async () => {
    campaignsApi.campaigns.set(
      '123e4567-e89b-12d3-a456-426614174000',
      campaign('123e4567-e89b-12d3-a456-426614174000'),
    );
    createShell('sources');

    fixture.detectChanges();
    await settle();

    expect(heading()).toBe('Ash Crown');
    expect(text()).toContain('Las fuentes aún no están disponibles');
    expect(currentSection()).toBe('Fuentes');
    expect(prohibitedControls()).toEqual([]);
  });

  it('renders malformed campaign route state before calling the API', async () => {
    route.setCampaignId('not-a-campaign-id');
    createShell('chat');

    fixture.detectChanges();
    await settle();

    expect(campaignsApi.requestedIds).toEqual([]);
    expect(heading()).toBe('Enlace de campaña no válido');
    expect(text()).toContain('Este enlace no corresponde a ninguna campaña.');
  });

  it('renders unknown campaign and recoverable API states', async () => {
    campaignsApi.error = { kind: 'notFound', code: 'campaign_not_found' };
    createShell('chat');

    fixture.detectChanges();
    await settle();

    expect(heading()).toBe('Campaña no encontrada');

    campaignsApi.error = { kind: 'recoverableError', code: 'network_error' };
    campaignsApi.requestedIds = [];
    route.setCampaignId('223e4567-e89b-12d3-a456-426614174000');
    await settle();

    expect(campaignsApi.requestedIds).toEqual(['223e4567-e89b-12d3-a456-426614174000']);
    expect(heading()).toBe('No se pudo cargar la campaña');
    expect(button('Reintentar')).toBeTruthy();
  });

  it('does not display the previous campaign while a new campaign id is resolving', async () => {
    campaignsApi.campaigns.set(
      '123e4567-e89b-12d3-a456-426614174000',
      campaign('123e4567-e89b-12d3-a456-426614174000'),
    );
    createShell('chat');

    fixture.detectChanges();
    await settle();
    expect(text()).toContain('Ash Crown');

    route.setCampaignId('223e4567-e89b-12d3-a456-426614174000');
    fixture.detectChanges();

    expect(text()).toContain('Cargando campaña…');
    expect(text()).not.toContain('Ash Crown');
  });

  function createShell(kind: 'chat' | 'sources'): void {
    fixture = TestBed.createComponent(CampaignWorkspaceShellComponent);
    fixture.componentRef.setInput('workspaceKind', kind);
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
    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }
});

class CampaignsApiStub {
  readonly campaigns = new Map<string, Campaign>();
  requestedIds: string[] = [];
  error: unknown;

  async getCampaign(id: string): Promise<Campaign> {
    this.requestedIds.push(id);

    if (this.error) {
      throw this.error;
    }

    const value = this.campaigns.get(id);

    if (!value) {
      throw { kind: 'notFound', code: 'campaign_not_found' };
    }

    return value;
  }
}

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

function campaign(id: string): Campaign {
  return {
    id,
    name: id.startsWith('123') ? 'Ash Crown' : 'Bright Vale',
    createdAt: '2026-09-15T10:00:00Z',
    updatedAt: '2026-09-15T10:00:00Z',
  };
}
