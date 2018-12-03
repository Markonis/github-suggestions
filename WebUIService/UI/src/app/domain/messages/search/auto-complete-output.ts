import { CachedItem } from '../../entities/cached-item';
import { Repository } from '../../entities/repository';

export class AutoCompleteOutput {
  constructor(public items: CachedItem<Repository>[]) { }
}
