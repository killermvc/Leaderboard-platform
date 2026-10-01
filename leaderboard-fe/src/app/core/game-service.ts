import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Game {
  id: number;
  name: string;
  description: string;
  imageUrl?: string;
  ownerId?: number | null;
  ownerUsername?: string | null;
  isSubmitAllowed: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class GameService {
  private readonly baseUrl = `${environment.apiUrl.replace(/\/auth$/, '')}/game`;

  constructor(private http: HttpClient) {}

  /**
   * Creates a new game (Admin only)
   * @param name The name of the game
   * @param description The description of the game
   * @param imageUrl The URL of the game's image
   * @returns Observable with the created game
   */
  createGame(name: string, description: string = '', imageUrl: string = '', ownerId: number | null = null): Observable<Game> {
    const body = {
      Name: name,
      Description: description,
      ImageUrl: imageUrl,
      OwnerId: ownerId,
      IsSubmitAllowed: true,
    };
    return this.http.post<Game>(this.baseUrl, body);
  }

  /**
   * Gets a game by its ID
   * @param id The game ID
   * @returns Observable with the game details
   */
  getGameById(id: number): Observable<Game> {
    return this.http.get<Game>(`${this.baseUrl}/${id}`);
  }

  updateGame(id: number, game: Pick<Game, 'name' | 'description' | 'imageUrl' | 'isSubmitAllowed'> & { ownerId?: number | null }): Observable<Game> {
    return this.http.put<Game>(`${this.baseUrl}/${id}`, {
      Name: game.name,
      Description: game.description,
      ImageUrl: game.imageUrl,
      OwnerId: game.ownerId,
      IsSubmitAllowed: game.isSubmitAllowed,
    });
  }

  /**
   * Gets all games with pagination
   * @param limit Maximum number of games to return
   * @param offset Number of games to skip
   * @returns Observable with an array of games
   */
  getAllGames(limit: number = 10, offset: number = 0): Observable<Game[]> {
    const params = new HttpParams()
      .set('limit', limit.toString())
      .set('offset', offset.toString());

    return this.http.get<Game[]>(this.baseUrl, { params });
  }

  /**
   * Gets all games played by a specific player
   * @param playerId The player's user ID
   * @returns Observable with an array of games
   */
  getGamesByPlayer(playerId: number): Observable<Game[]> {
    return this.http.get<Game[]>(`${this.baseUrl}/player/${playerId}`);
  }
}
