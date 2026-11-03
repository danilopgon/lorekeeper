import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { SourcesShellPage } from './sources-shell.page';

describe('SourcesShellPage', () => {
  let fixture: ComponentFixture<SourcesShellPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SourcesShellPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: route('123e4567-e89b-12d3-a456-426614174000'),
        },
      ],
    }).compileComponents();
  });

  it('renders the Sources unavailable route shell', async () => {
    fixture = TestBed.createComponent(SourcesShellPage);

    fixture.detectChanges();
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/campaigns/123e4567-e89b-12d3-a456-426614174000')
      .flush({
        id: '123e4567-e89b-12d3-a456-426614174000',
        name: 'Ash Crown',
        createdAt: '2026-09-15T10:00:00Z',
        updatedAt: '2026-09-15T10:00:00Z',
      });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Las fuentes aún no están disponibles',
    );
  });
});

function route(campaignId: string) {
  const paramMap = convertToParamMap({ campaignId });
  return { paramMap: of(paramMap), snapshot: { paramMap } };
}
