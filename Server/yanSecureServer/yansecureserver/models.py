from sqlalchemy import (
    Column,
    Integer,
    Text,
    DateTime,
    desc
)

from datetime import datetime, timedelta
from sqlalchemy.ext.declarative import declarative_base

from sqlalchemy.orm import scoped_session, sessionmaker

from zope.sqlalchemy import ZopeTransactionExtension

DBSession = scoped_session(sessionmaker(extension=ZopeTransactionExtension()))
Base = declarative_base()

class User(Base):
    __tablename__ = 'users'
    id = Column(Integer, primary_key=True)
    name = Column(Text)
    ip = Column(Text)
    port = Column(Integer)
    chatroom = Column(Text)
    last_ping = Column(DateTime)


    def __init__(self, name):
        self.name = name

    def update(self, ip, port, chatroom):
        self.ip = ip
        self.port = port
        self.chatroom = chatroom
        self.ping()

    def ping(self):
        self.last_ping = datetime.now()

    @classmethod
    def by_name(cls, name):
        user = DBSession.query(User).filter(User.name == name).first()
        if user is not None:
            user.ping()
        return user

    @property
    def peer(self):
        peer = DBSession.query(User).filter(
            (User.id != self.id) &
            (User.chatroom == self.chatroom) &
            (User.last_ping > datetime.now() - timedelta(seconds=5))
        ).order_by(desc(User.last_ping)).first()
        return peer