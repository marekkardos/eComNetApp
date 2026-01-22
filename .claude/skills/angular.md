# Angular 9 Review Skill

**Target version: Angular 9.x (Ivy renderer)**

## Component Design
- Smart vs presentational separation
- OnPush change detection candidates
- Input/Output binding correctness
- Lifecycle hooks (OnInit vs constructor, OnDestroy cleanup)
- Component size (split if >200 lines)
- No standalone components (Angular 14+ feature)

## Template Checks
- TrackBy in all ngFor (critical for performance)
- Async pipe vs manual subscribe
- No complex logic in templates
- Safe navigation operator (?.) usage
- No @if/@for syntax (use *ngIf/*ngFor)

## Module Organization (NgModule-based)
- Every component declared in exactly one module
- Feature modules properly bounded
- Lazy loading via loadChildren with string path or import()
- SharedModule for common components/pipes/directives
- CoreModule for singleton services (imported once in AppModule)
- No circular module dependencies

## Dependency Injection
- providedIn: 'root' for app-wide singletons
- Module providers array for scoped services
- Injection token (InjectionToken) for interfaces
- No service instantiation with `new`
- Constructor injection only (no inject() function)

## Angular 9 Specific
- Ivy is default but check for ViewEngine compatibility issues
- No ES5 browser support without differential loading
- @angular/common/http (not deprecated @angular/http)
- TestBed.configureTestingModule patterns

## Forms
- Reactive forms preferred (FormGroup, FormControl, FormArray)
- Template-driven only for simple cases
- Proper validation patterns
- FormBuilder injection for cleaner code
