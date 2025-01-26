import os
import uvicorn
import fastapi
import fastapi.responses 
import fastapi.staticfiles
import fastapi.templating
from fastapi.responses import FileResponse

from fastapi import Depends, FastAPI, HTTPException, status
from fastapi.security import OAuth2PasswordBearer, OAuth2PasswordRequestForm
from typing_extensions import Annotated
from pydantic import BaseModel, parse_obj_as
from datetime import datetime, timedelta
from passlib.context import CryptContext

import fakedb

try:
	import jwt
except ModuleNotFoundError:
	from jose import JWTError, jwt 

# necessary variables:

app = fastapi.FastAPI()

sourceFileDir = os.path.dirname(os.path.abspath(__file__))

SECRET_KEY = "a3ebf96b20a9d024af7f261b9e3e4f8a37714e81e22a551994882ac55d989a26"
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPITE_MINUTES = 120 #30

db = fakedb.get_user_data()

class Token(BaseModel):
	access_token: str
	token_type: str

class TokenData(BaseModel):
	username: str or None = None   # type: ignore

class User(BaseModel):
	ID: int
	username: str
	email: str or None = None      # type: ignore
	full_name: str or None = None  # type: ignore
	disabled: bool or None = None  # type: ignore

class UserInDB(User):
	hashed_password: str

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="token")

# authentication functions:

def verify_password(plain_password, hashed_password):
	return pwd_context.verify(plain_password, hashed_password)

def get_password_hash(password):
	return pwd_context.hash(password)

def get_user(db, username:str):

		if username in db:
			user_dict = db[username]
			return UserInDB(**user_dict)



def authenticate_user(db, username:str, password:str):
	user = get_user(db, username)
	if not user:
		return False
	if not verify_password(password, user.hashed_password):
		return False

	return user

def create_access_token(data: dict, expires_delta: timedelta or None = None): # type: ignore
	to_encode = data.copy()
	if expires_delta:
		expire = datetime.utcnow() + expires_delta
	else:
		expire = datetime.utcnow() + timedelta(minutes=15)

	to_encode.update({"exp": expire})
	encoded_jwt = jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)
	return encoded_jwt

async def get_current_user(token:str = Depends(oauth2_scheme)):
	credential_exception = HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Could not validate credential", headers={"WWW-Authenticate": "Bearer"})

	try:
		payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
		username: str = payload.get("sub")
		if username is None:
			raise credential_exception

		token_data = TokenData(username=username)
	except JWTError:
		raise credential_exception

	user = get_user(db, username=token_data.username)
	if user is None:
		raise credential_exception

	return user

async def get_current_active_user(current_user: UserInDB = Depends(get_current_user)):
	if current_user.disabled:
		raise HTTPException(status_code=400, detail="Inactive user")

	return current_user

# authentication routes:

@app.post("/token", response_model=Token)
async def login_for_access_token(form_data: OAuth2PasswordRequestForm = Depends()):
	user = authenticate_user(db, form_data.username, form_data.password)
	if not user:
		raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Incorrect username or password", headers={"WWW-Authenticate": "Bearer"})

	access_token_expires = timedelta(minutes=ACCESS_TOKEN_EXPITE_MINUTES)
	accsess_token = create_access_token(data={"sub": user.username}, expires_delta=access_token_expires)
	return {"access_token": accsess_token, "token_type": "Bearer"}


# application routes:

chat = [["tim", "test"], ["timoty", "test2"], ["", ""], ["", ""], ["", ""], ["", ""]]

def move_chat(message):
	chat.insert(0, message)
	chat.pop(-1)
	
class message(BaseModel):
	msg: str

@app.post("/newmsg")
def new_message(msg: message, current_user: User = Depends(get_current_active_user)):
	move_chat([current_user.username, msg.msg])
	return fastapi.responses.JSONResponse(content=chat)

@app.get("/getchat")
async def get_chat(current_user: User = Depends(get_current_active_user)):

	chat_send = []

	for message in chat:
		if message[0] == current_user.username:
			chat_send.append(["", message[1], message[0]])
		else:
			chat_send.append([message[0], message[1], ""])
	return fastapi.responses.JSONResponse(content=chat_send)

if __name__ == "__main__":
    uvicorn.run("main:app", port=8000, reload=True)