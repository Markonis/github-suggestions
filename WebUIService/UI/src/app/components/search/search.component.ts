import { Component, OnInit } from '@angular/core';
import { SearchService } from 'src/app/services/search.service';
import { SearchInput } from 'src/app/domain/messages/search/search-input';
import { SessionService } from 'src/app/services/session.service';
import { OauthService } from 'src/app/services/oauth.service';

@Component({
  selector: 'app-search',
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.less']
})
export class SearchComponent implements OnInit {

  constructor(
    public searchService: SearchService,
    public sessionService: SessionService,
    public oauthService: OauthService) { }

  ngOnInit() {
    this.sessionService.getStatus();
    this.oauthService.createUrl();
  }

  performSearch(query: string) {
    this.searchService.search(new SearchInput(query));
  }

  autoComplete(query: string) {
    this.searchService.autocomplete(new SearchInput(query));
  }
}
