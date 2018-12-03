import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { SearchOutput } from '../domain/messages/search/search-output';
import { SearchInput } from '../domain/messages/search/search-input';
import { AutoCompleteOutput } from '../domain/messages/search/auto-complete-output';

@Injectable({
  providedIn: 'root'
})
export class SearchService {

  searchOutput: SearchOutput = null;
  suggestOutput: SearchOutput = null;
  autoCompleteOutput: AutoCompleteOutput = null;

  constructor(private _api: ApiService) { }

  search(input: SearchInput) {
    this._api.search(input).subscribe(result => {
      if (result.success) { this.searchOutput = result.data; }
    });

    this._api.suggest(input).subscribe(result => {
      this.suggestOutput = result;
    });
  }

  autocomplete(input: SearchInput) {
    if (input.query.length > 0) {
      this._api.autoComplete(input).subscribe(output => {
        this.autoCompleteOutput = output;
      });
    } else {
      this.autoCompleteOutput = null;
    }
  }
}
