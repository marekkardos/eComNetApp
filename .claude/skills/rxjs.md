# RxJS 6.x Review Skill

**Target version: RxJS 6.x (Angular 9 compatible)**

## Subscription Management
- Every subscribe() must have corresponding unsubscribe
- Use takeUntil pattern with destroy$ subject:
  ```typescript
  private destroy$ = new Subject<void>();
  
  ngOnInit() {
    this.data$.pipe(takeUntil(this.destroy$)).subscribe(...);
  }
  
  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
  ```
- Async pipe eliminates manual subscription needs
- No takeUntilDestroyed() (Angular 16+ feature)

## Import Paths (RxJS 6.x)
- Operators from 'rxjs/operators': `import { map, filter } from 'rxjs/operators'`
- Observables from 'rxjs': `import { Observable, Subject, of } from 'rxjs'`
- No rxjs-compat (migration helper, should be removed)

## Anti-patterns
- Nested subscribes → use switchMap/mergeMap/concatMap
- subscribe() inside subscribe() is always wrong
- Avoid toPromise() when Observable works (deprecated path)
- Don't mix callbacks and Observables

## Operator Selection
- switchMap: cancel previous (HTTP search, autocomplete)
- mergeMap: parallel execution (fire-and-forget)
- concatMap: sequential, preserve order (queue)
- exhaustMap: ignore while busy (form submit, prevent double-click)

## Error Handling
- catchError at appropriate level (not too high, not too low)
- Don't swallow errors silently: `catchError(err => { log(err); return EMPTY; })`
- Retry strategies: retry(3) or retryWhen for custom logic
- Error state propagation to UI

## Memory Leaks (Critical for Angular 9)
- Subjects must complete on destroy
- HTTP observables complete automatically (but takeUntil still safer)
- Router events do NOT complete - must unsubscribe
- FormControl valueChanges do NOT complete - must unsubscribe
- BehaviorSubject vs ReplaySubject choice matters for late subscribers

## Performance
- distinctUntilChanged to prevent unnecessary emissions
- debounceTime(300) for user input (search boxes)
- throttleTime for scroll/resize events
- share() or shareReplay() to prevent multiple HTTP calls
- shareReplay({ bufferSize: 1, refCount: true }) for caching with cleanup
