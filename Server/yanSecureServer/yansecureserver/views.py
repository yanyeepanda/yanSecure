from pyramid.view import view_config
from .models import DBSession, User

class WebService(object):

    def __init__(self, request):
        self.request = request

    def __get_user(self, name):
        user = User.by_name(name)
        if user is None:
            user = User(name)
            DBSession.add(user)
            DBSession.flush()
        return user

    @view_config(route_name='connect', renderer="json")
    def connect(self):
        # Get input parameters
        name = self.request.params.get("name")
        chatroom = self.request.params.get("chatroom")
        port = self.request.params.get("port")
        ip = self.request.remote_addr

        # If all data provided
        if None not in (name, chatroom, port):
            # Get (or create) user, then update
            user = self.__get_user(name)
            user.update(ip, port, chatroom)

            # Check to see if the user has a peer
            peer = user.peer
            if peer is not None:
                data = dict(name=peer.name, ip_address=peer.ip, port=peer.port)
                return dict(data=data, success=True)

        return dict(success=False)
