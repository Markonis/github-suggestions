export class CachedItem<T>{
  constructor(public cachedAt: string, public data: T) { }
}
