import ChatIcon from '@mui/icons-material/Chat';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import Logout from '@mui/icons-material/Logout';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import SearchIcon from '@mui/icons-material/Search';
import SendIcon from '@mui/icons-material/Send';
import Settings from '@mui/icons-material/Settings';
import { Avatar, Badge, Box, Button, Divider, FormControl, IconButton, InputAdornment, ListItemIcon, Menu, MenuItem, OutlinedInput, Paper, Stack, Tab, Tabs, TextField, Typography } from '@mui/material';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import Cookies from 'js-cookie';
import React, { useContext, useEffect, useRef, useState } from 'react';
import InputEmoji from "react-input-emoji";
import { useDispatch, useSelector } from 'react-redux';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { SignalRContext } from '~/Context/SignalRContext';
import services from '~/plugins/services';
import { setUserInformation, userInfor } from '~/redux/features/userSlice';
import PAGES from '~/utils/pages';
import ButtonComponent from '../ButtonComponent';
import Logo from '../Logo';
import NavigationMobile from './NavigationMobile';
import { jwtDecode } from 'jwt-decode';
dayjs.locale('vi');
function Header() {
    const [tab, setTab] = useState("1");
    const [anchorEl, setAnchorEl] = React.useState(null);
    const [accountMenu, setAccountMenu] = React.useState(null);
    const [anchorEl1, setAnchorEl1] = React.useState(null);
    const nav = useNavigate();
    const userInfo = useSelector(userInfor);
    const openAccountMenu = Boolean(accountMenu);
    const [searchVal, setSearchVal] = useState('');
    const dispatch = useDispatch();
    const location = useLocation();
    const [openNotification, setOpenNotification] = useState(false);
    const { connection, openMessage, setOpenMessage, setCurrentChat, currentChat, conversations, setConversations } = useContext(SignalRContext);
    const [notifications, setNotifications] = useState([]);
    const notificationRef = useRef(null);
    const notificationIconRef = useRef(null);
    const [unreadNoti, setUnreadNoti] = useState(0);
    dayjs.extend(relativeTime);
    const [text, setText] = useState("");
    const [messages, setMessages] = useState([]);
    const [chatBox, setChatBox] = useState(null);
    const messageIconRef = useRef(null);
    const [unreadMessage, setUnreadMessage] = useState(true);
    const [newMessage, setNewMessage] = useState(null);
    const [newNotification, setNewNotification] = useState(null);
    useEffect(() => {
        if (location.pathname.includes("/home-page")) {
            setTab("1");
        } else if (location.pathname.includes("/list-tutor")) {
            setTab("2");
        } else if (location.pathname.includes("/my-tutor")) {
            setTab("2");
        } else if (location.pathname.includes("/request-history")) {
            setTab("2");
        }
        else if (location.pathname.includes("/my-childlren")) {
            setTab("3");
        }
        else if (location.pathname.includes("/list-blogs")) {
            setTab("4");
        }
        else if (location.pathname.includes("/test")) {
            setTab("5");
        }
    }, [location])

    useEffect(() => {
        const accessToken = Cookies.get("access_token");
        if (!accessToken) {
            return;
        }
        const decodedToken = jwtDecode(accessToken);
        const role = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        if (!location.pathname.includes(PAGES.ROOT + PAGES.LOGIN) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.REGISTER) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.FORGOTPASSWORD) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.RESETPASSWORD) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.LISTTUTOR) &&
            !location.pathname.includes("/autismedu/tutor-profile/") &&
            !location.pathname.includes(PAGES.ROOT + PAGES.CONFIRMREGISTER) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.LOGIN_OPTION) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.REGISTER_OPTION) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.CHANGE_PASSWORD) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.BLOG_LIST) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.BLOG_LIST) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.BLOG_DETAIL) &&
            !location.pathname.includes(PAGES.ROOT + PAGES.HOME)
        ) {
            if (userInfo === undefined || role !== "Parent") {
                nav(PAGES.ROOT + PAGES.LOGIN)
            }
        }
        if (userInfo) {
            handleGetNotification();
            handleGetConversation();
        }
    }, [userInfo, location])

    useEffect(() => {
        if (openMessage && !currentChat && conversations.length !== 0) {
            setCurrentChat(conversations[0]);
        }
    }, [openMessage])

    useEffect(() => {
        if (newMessage) {
            if (currentChat && newMessage.conversation.id === currentChat?.id)
                setMessages((preMessages) => [...preMessages, newMessage])
            const receiveConversation = conversations.find((c) => {
                return c.id === newMessage.conversation.id
            })
            if (receiveConversation) {
                receiveConversation.messages = [newMessage];
                const filtedConversation = conversations.filter((c) => {
                    return c.id !== newMessage.conversation.id;
                })
                if (receiveConversation.id !== currentChat?.id && receiveConversation.isRead === true) {
                    receiveConversation.isRead = false;
                    setUnreadMessage(false);
                }
                if (receiveConversation.id === currentChat?.id) {
                    handleReadMessage();
                    if (chatBox) {
                        chatBox.scrollTop = chatBox.scrollHeight;
                    }
                }
                setConversations([receiveConversation, ...filtedConversation])
            } else {
                let read = false;
                if (currentChat) {
                    read = false;
                } else read = true;
                setConversations([
                    {
                        id: newMessage.conversation.id,
                        user: newMessage.sender,
                        messages: [newMessage],
                        isRead: read
                    }, ...conversations
                ])
                if (openMessage && !currentChat) {
                    setCurrentChat({
                        id: newMessage.conversation.id,
                        user: newMessage.sender,
                        messages: [newMessage],
                        isRead: true
                    })
                } else {
                    setUnreadMessage(false)
                }
            }
        }
    }, [newMessage])

    useEffect(() => {
        if (newNotification) {
            setNotifications((preNotifications) => [newNotification, ...preNotifications]);
            setUnreadNoti(unreadNoti + 1)
        }
    }, [newNotification])

    useEffect(() => {
        if (chatBox && messages.length <= 10) {
            chatBox.scrollTop = chatBox.scrollHeight;
        }
    }, [messages, chatBox]);
    useEffect(() => {
        if (!connection || !userInfo) return;
        connection.on(`Notifications-${userInfo.id}`, (notification) => {
            console.log(notification);
            setNewNotification(notification)
        });
        connection.on(`Messages-${userInfo.id}`, (message) => {
            console.log(message);
            setNewMessage(message)
        });
        return () => {
            connection.off(`Notifications-${userInfo.id}`);
            connection.off(`Messages-${userInfo.id}`);
        };
    }, [connection, userInfo]);

    useEffect(() => {
        if (currentChat && currentChat?.id !== 0) {
            handleGetMessage();
            handleReadMessage();
        } else setMessages([])
        setText("");
    }, [currentChat])
    const handleGetConversation = async () => {
        try {
            await services.ConversationAPI.getConversations((res) => {
                const returnArr = res.result.map((r) => {
                    if (r.messages[0].sender.id === userInfo.id) {
                        r.isRead = true;
                    } else {
                        r.isRead = r.messages[0].isRead;
                    }
                    return r;
                })
                returnArr.forEach((r) => {
                    if (r.isRead === false) {
                        setUnreadMessage(false);
                    }
                })
                setConversations(returnArr);
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: 1
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleReadMessage = async () => {
        try {
            await services.MessageAPI.readMessages(currentChat?.id || 0, {}, (res) => {
                const currentChatBox = conversations.find((c) => {
                    return c.id === currentChat?.id
                })
                currentChatBox.isRead = true;
                setUnreadMessage(true);
                conversations.forEach((r) => {
                    if (r.isRead === false) {
                        setUnreadMessage(false);
                    }
                })
                setConversations([...conversations]);
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: 1
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleGetMessage = async () => {
        try {
            await services.MessageAPI.getMessages(currentChat?.id || 0, (res) => {
                setMessages(res.result.reverse());
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: 1
            })
        } catch (error) {
            console.log(error);
        }
    }
    useEffect(() => {
        if (currentChat) {
            handleGetMessage();
        }
    }, [currentChat])
    const handleGetNotification = async () => {
        try {
            await services.NotificationAPI.getAllPaymentPackage((res) => {
                const filterArr = res.result.result.filter((r, index) => {
                    return index <= 10
                })
                setNotifications([...notifications, ...filterArr]);
                setUnreadNoti(res.result.totalUnRead);
            }, (error) => {
                console.log(error);
            }, {
                pageNumber: notifications.length === 0 ? 1 : 2,
                pageSize: notifications.length <= 10 ? 10 : notifications.length
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleOpenAccountMenu = (event) => {
        setAccountMenu(event.currentTarget);
    };
    const handleCloseAccountMenu = () => {
        setAccountMenu(null);
    };

    const open = Boolean(anchorEl);
    const handleClickListItem = (event) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuItemClick = (event, link) => {
        nav(link)
        setAnchorEl(null);
    };

    const handleClose = () => {
        setAnchorEl(null);
    };

    const open1 = Boolean(anchorEl1);
    const handleClickListItem1 = (event) => {
        setAnchorEl1(event.currentTarget);
    };

    const handleMenuItemClick1 = (event, link) => {
        nav(link)
        setAnchorEl1(null);
    };

    const handleClose1 = () => {
        setAnchorEl1(null);
    };

    const handleLogout = () => {
        Cookies.remove("access_token");
        Cookies.remove("refresh_token");
        dispatch(setUserInformation(null))
        nav(PAGES.ROOT + PAGES.HOME)
    };

    const handleSearch = () => {
        nav('/autismedu/list-tutor', { state: { searchVal } });
        setSearchVal('');
    };
    const handleClickOutside = (event) => {
        if (
            messageIconRef.current &&
            !messageIconRef.current.contains(event.target) &&
            !event.target.closest(".MuiIconButton-root")
        ) {
            setOpenMessage(false);
        }
        if (
            notificationRef.current &&
            !notificationRef.current.contains(event.target) &&
            !notificationIconRef.current.contains(event.target) &&
            !event.target.closest(".MuiIconButton-root")
        ) {
            setOpenNotification(false);
        }
    };
    useEffect(() => {
        document.addEventListener("mousedown", handleClickOutside);
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, []);

    const handleReadOne = async (id) => {
        try {
            await services.NotificationAPI.readAPaymentPackage(id, {}, (res) => {
                const selectedNoti = notifications.find((n) => {
                    return n.id === id;
                })
                selectedNoti.isRead = true;
                setNotifications([...notifications]);
                setUnreadNoti(unreadNoti - 1)
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }
    const handleReadAll = async () => {
        try {
            await services.NotificationAPI.readAllPaymentPackage({}, (res) => {
                notifications.forEach((n) => {
                    n.isRead = true;
                })
                setNotifications([...notifications]);
                setUnreadNoti(0)
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }

    const sendMessages = async () => {
        if (text.trim() === "") return;
        try {
            await services.MessageAPI.sendMessages({
                conversationId: currentChat?.id,
                content: text.trim(),
            }, (res) => {
                setMessages([...messages, res.result]);
                const receiveConversation = conversations.find((c) => {
                    return c.id === res.result.conversation.id
                })

                receiveConversation.messages = [res.result];
                const filtedConversation = conversations.filter((c) => {
                    return c.id !== res.result.conversation.id;
                })
                setConversations([receiveConversation, ...filtedConversation])
                setText("");
                if (chatBox) {
                    chatBox.scrollTop = chatBox.scrollHeight;
                }
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }
    return (
        <Stack
            direction="row"
            spacing={3}
            sx={{
                justifyContent: "space-between", alignItems: "center", position: "fixed", top: "0px",
                height: "80px", width: "100vw", px: "20px",
                zIndex: "10",
                bgcolor: "white",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
            <Logo sizeLogo="50px" sizeName="35px" />
            <Tabs value={tab} sx={{
                display: {
                    lg: "block",
                    xs: "none"
                }
            }}>
                <Tab sx={{ fontSize: "18px" }} value={"1"} label="Trang chủ" onClick={() => { nav(PAGES.ROOT + PAGES.HOME) }} />
                <Tab sx={{ fontSize: "18px" }} value={"2"} label="Gia sư" icon={<ExpandMoreIcon />} iconPosition="end" onClick={handleClickListItem} />
                {
                    userInfo && (
                        <Tab sx={{ fontSize: "18px" }} value={"3"} label="Thông tin trẻ" onClick={() => { nav(PAGES.ROOT + PAGES.MY_CHILDREN) }} />
                    )
                }
                <Tab sx={{ fontSize: "18px" }} value={"4"} label="Blog" onClick={() => { nav(PAGES.ROOT + PAGES.BLOG_LIST) }} />
                {/* <Tab sx={{ fontSize: "18px" }} value={"5"} label="Kiểm tra" onClick={() => { nav(PAGES.ROOT + PAGES.TEST) }} /> */}
                {/* {userInfo && <Tab sx={{ fontSize: "18px" }} value={"5"} label="Kiểm tra" icon={<ExpandMoreIcon />} iconPosition="end" onClick={handleClickListItem1} />} */}

            </Tabs>
            <Menu
                id="lock-menu"
                anchorEl={anchorEl}
                open={open}
                onClose={handleClose}
                MenuListProps={{
                    'aria-labelledby': 'lock-button',
                    role: 'listbox'
                }}
            >
                {
                    userInfo && (
                        <MenuItem onClick={(event) => handleMenuItemClick(event, PAGES.ROOT + PAGES.MY_TUTOR)}>
                            Gia sư của tôi
                        </MenuItem>
                    )
                }
                <MenuItem
                    onClick={(event) => handleMenuItemClick(event, PAGES.ROOT + PAGES.LISTTUTOR)}
                >
                    Danh sách gia sư
                </MenuItem>
                {userInfo && (<MenuItem
                    onClick={(event) => handleMenuItemClick(event, PAGES.ROOT + PAGES.TUTOR_REQUEST_HISTORY)}
                >
                    Lịch sử yêu cầu
                </MenuItem>)}
            </Menu>

            <Menu
                id="lock-menu"
                anchorEl={anchorEl1}
                open={open1}
                onClose={handleClose1}
                MenuListProps={{
                    'aria-labelledby': 'lock-button',
                    role: 'listbox'
                }}
            >
                <MenuItem
                    onClick={(event) => handleMenuItemClick1(event, PAGES.ROOT + PAGES.TEST)}
                >
                    Danh sách bài kiểm tra
                </MenuItem>
                {(<MenuItem
                    onClick={(event) => handleMenuItemClick1(event, PAGES.ROOT + PAGES.TEST_HISTORY)}
                >
                    Lịch sử bài kiểm tra
                </MenuItem>)}
            </Menu>

            <Stack direction="row" sx={{ alignItems: "center" }} spacing={2}>
                <TextField
                    variant="outlined"
                    placeholder="Hãy tên gia sư mà bạn muốn tìm..."
                    value={searchVal}
                    onChange={(e) => setSearchVal(e.target.value)}
                    fullWidth
                    InputProps={{
                        endAdornment: (
                            <InputAdornment position="end">
                                <IconButton onClick={handleSearch}>
                                    <SearchIcon />
                                </IconButton>
                            </InputAdornment>
                        ),
                        sx: {
                            width: '350px',
                            height: '45px',
                            borderRadius: '999px',
                            backgroundColor: '#fff'
                        }
                    }}
                />

                {
                    userInfo && (
                        <Box sx={{ position: "relative" }}>
                            <IconButton sx={{ color: "#c58ee5" }}
                                onClick={(e) => {
                                    e.stopPropagation();
                                    setOpenMessage(!openMessage);
                                    setOpenNotification(false);
                                }}>
                                <Badge variant="dot" invisible={unreadMessage} color="primary">
                                    <ChatIcon />
                                </Badge>
                            </IconButton>
                            {
                                openMessage && (
                                    <Paper variant='elevation' sx={{
                                        position: "absolute", top: "50px",
                                        right: "0px", width: "1000px", bgcolor: "#F3E8FF",
                                        p: 2,
                                        height: "85vh"
                                    }} ref={messageIconRef}>
                                        {
                                            conversations.length !== 0 ? (
                                                <Stack direction='row' sx={{ height: "100%" }} gap={1}>
                                                    <Box sx={{ width: "35%" }}>
                                                        <Typography variant='h4'>Đoạn chat</Typography>
                                                        <Box sx={{
                                                            overflow: "hidden", height: "80%",
                                                            "&:hover": {
                                                                overflow: "auto"
                                                            },
                                                            mt: 1
                                                        }}>
                                                            {
                                                                conversations && conversations.length !== 0 && conversations.map((c) => {
                                                                    return (
                                                                        <Stack key={c.id} direction='row' gap={1} alignItems="center" sx={{
                                                                            py: 2, justifyContent: "space-between",
                                                                            cursor: "pointer",
                                                                            px: 2,
                                                                            bgcolor: c.id === currentChat?.id ? "#F8F0FF" : "",
                                                                            borderRadius: "10px",
                                                                            ":hover": {
                                                                                bgcolor: "#F8F0FF"
                                                                            }
                                                                        }} onClick={() => setCurrentChat(c)}>
                                                                            <Stack direction='row' gap={2} alignItems="center" sx={{ width: "80%" }}>
                                                                                <Avatar alt="Remy Sharp" src={c.user.imageUrl} />
                                                                                <Box sx={{ overflow: "hidden" }}>
                                                                                    <Typography fontWeight="bold" color="black">{c.user.fullName}</Typography>
                                                                                    <Typography sx={{
                                                                                        display: '-webkit-box',
                                                                                        WebkitLineClamp: 1,
                                                                                        WebkitBoxOrient: 'vertical',
                                                                                        overflow: 'hidden',
                                                                                        textOverflow: 'ellipsis'
                                                                                    }}>{c.messages[0].content}</Typography>
                                                                                </Box>
                                                                            </Stack>
                                                                            {
                                                                                !c.isRead && (
                                                                                    <Box sx={{ width: "15px", height: "15px", bgcolor: "blue", borderRadius: "50%" }}>
                                                                                    </Box>
                                                                                )
                                                                            }
                                                                        </Stack>
                                                                    )
                                                                })
                                                            }
                                                        </Box>
                                                    </Box>
                                                    <Divider orientation="vertical" flexItem />

                                                    <Stack direction='column' sx={{ width: "65%", height: "100%" }}>
                                                        <Stack direction='row' sx={{
                                                            px: 1, alignItems: "center",
                                                            justifyContent: "space-between"
                                                        }}>
                                                            <Stack direction='row' sx={{ gap: 2, alignItems: "center" }}>
                                                                <Avatar alt="Remy Sharp" src={currentChat ? currentChat?.user?.imageUrl : "/"} sx={{
                                                                    width: "50px",
                                                                    height: "50px"
                                                                }} />
                                                                <Typography variant='h5' sx={{}}>{currentChat ? currentChat?.user?.fullName : "Mất kết nối"}</Typography>
                                                            </Stack>
                                                        </Stack>
                                                        <Divider sx={{ mt: 1 }} />
                                                        <Box style={{
                                                            width: "100%",
                                                            flexGrow: 2,
                                                            overflow: "auto"
                                                        }} ref={setChatBox}>
                                                            {
                                                                messages && messages.length !== 0 && messages.map((m) => {
                                                                    if (m.sender?.id === userInfo?.id) {
                                                                        return (
                                                                            <Stack key={m.id} direction='row' sx={{
                                                                                justifyContent: "flex-end",
                                                                                mt: 1
                                                                            }}>
                                                                                <Box sx={{
                                                                                    bgcolor: "#E0D1FF",
                                                                                    p: 2,
                                                                                    borderRadius: "15px",
                                                                                    maxWidth: "70%"
                                                                                }}>
                                                                                    <Typography sx={{ whiteSpace: "normal", wordBreak: "break-word" }}>{m.content}</Typography>
                                                                                </Box>
                                                                            </Stack>
                                                                        )
                                                                    } else {
                                                                        return (
                                                                            <Stack direction="row" key={m.id} sx={{ mt: 1 }}>
                                                                                <Box sx={{
                                                                                    bgcolor: "#FBF8FF",
                                                                                    p: 2,
                                                                                    borderRadius: "15px",
                                                                                    maxWidth: "70%",
                                                                                    flexShrink: 0
                                                                                }}>
                                                                                    <Typography sx={{ whiteSpace: "normal", wordBreak: "break-word" }}>{m.content}</Typography>
                                                                                </Box>
                                                                            </Stack>
                                                                        )
                                                                    }
                                                                })
                                                            }
                                                        </Box>
                                                        <Divider sx={{ mt: 1 }} />
                                                        <Stack direction='row' justifyContent='space-between' alignItems="center"
                                                            sx={{
                                                                maxHeight: "110px"
                                                            }}>
                                                            <Box flexGrow={2} sx={{ whiteSpace: "normal", wordBreak: "break-word" }}>
                                                                <InputEmoji
                                                                    value={text}
                                                                    onChange={setText}
                                                                    cleanOnEnter
                                                                    onKeyDown={(event) => {
                                                                        if (event.key === "Enter") {
                                                                            sendMessages();
                                                                        }
                                                                    }}
                                                                    placeholder="Nhập tin nhắn vào đây"
                                                                />
                                                            </Box>
                                                            <IconButton onClick={() => sendMessages()} sx={{
                                                                bgcolor: "#c079ea", color: "white",
                                                                ":hover": {
                                                                    bgcolor: "#c58ee5"
                                                                }
                                                            }}>
                                                                <SendIcon />
                                                            </IconButton>
                                                        </Stack>
                                                    </Stack>
                                                </Stack>
                                            ) : (
                                                <Box sx={{
                                                    width: "100%",
                                                    height: "100%",
                                                    display: "flex",
                                                    justifyContent: "center",
                                                    alignItems: "center"
                                                }}>
                                                    <Typography sx={{ fontSize: "30px" }}>Bạn chưa có cuộc hội thoại nào</Typography>
                                                </Box>
                                            )
                                        }
                                    </Paper>
                                )
                            }
                        </Box>
                    )
                }
                {
                    userInfo && (
                        <Box sx={{ position: "relative" }}>
                            <IconButton sx={{ color: "#ff7900" }}
                                onClick={() => setOpenNotification(!openNotification)} ref={notificationIconRef}>
                                <Badge badgeContent={unreadNoti} color="primary">
                                    <NotificationsActiveIcon />
                                </Badge>
                            </IconButton>
                            {
                                openNotification && (
                                    <Paper variant='elevation' sx={{
                                        position: "absolute", top: "50px",
                                        right: "0px", width: "400px", overflow: 'auto', maxHeight: "70vh", bgcolor: "#f1f1f1"
                                    }} ref={notificationRef}>
                                        <Typography variant='h4' p={2}>Thông báo</Typography>
                                        <Button sx={{ p: 2 }} onClick={handleReadAll}>Đánh dấu đọc tất cả</Button>
                                        {
                                            notifications.length !== 0 && notifications.map((n) => {
                                                return (
                                                    <Link to={n.urlDetail} key={n.id}>
                                                        <Box sx={{
                                                            display: "flex", alignItems: "center", justifyContent: 'space-between',
                                                            p: 2, cursor: "pointer",
                                                            ":hover": {
                                                                bgcolor: "white"
                                                            }
                                                        }} onClick={() => {
                                                            if (!n.isRead)
                                                                handleReadOne(n.id)
                                                        }
                                                        }>
                                                            <Box sx={{ width: "90%" }}>
                                                                <Typography>
                                                                    {n.message}
                                                                </Typography>
                                                                <Typography color={"#556cd6"} fontSize={"12px"}>{dayjs(new Date(n.createdDate)).fromNow()}</Typography>
                                                            </Box>
                                                            {
                                                                !n.isRead && (
                                                                    <Box sx={{ borderRadius: "50%", width: "15px", height: "15px", bgcolor: "#556cd6" }}>
                                                                    </Box>
                                                                )
                                                            }
                                                        </Box>
                                                    </Link>
                                                )
                                            })
                                        }
                                        <Box textAlign="center">
                                            <Button>Xem thêm</Button>
                                        </Box>
                                    </Paper>
                                )
                            }
                        </Box>
                    )
                }
                {
                    !userInfo ? (
                        <>
                            <Box sx={{
                                display: {
                                    xs: "none",
                                    lg: "block"
                                }
                            }}>
                                <Link to={PAGES.ROOT + PAGES.LOGIN_OPTION}><ButtonComponent text="Đăng nhập" height="40px" /></Link>
                            </Box>
                            <Link to={PAGES.ROOT + PAGES.REGISTER_OPTION}><Button variant='outlined' sx={{
                                display: {
                                    xs: "none",
                                    lg: "block"
                                },
                                width: '100px',
                                height: "40px"
                            }}>Đăng ký</Button>
                            </Link>
                        </>
                    ) : (
                        <>
                            <Avatar alt="Remy Sharp" src={userInfo.imageUrl}
                                onClick={handleOpenAccountMenu} sx={{ cursor: "pointer" }} />
                            <Menu
                                anchorEl={accountMenu}
                                id="account-menu"
                                open={openAccountMenu}
                                onClose={handleCloseAccountMenu}
                                onClick={handleCloseAccountMenu}
                                slotProps={{
                                    paper: {
                                        elevation: 0,
                                        sx: {
                                            overflow: 'visible',
                                            filter: 'drop-shadow(0px 2px 8px rgba(0,0,0,0.32))',
                                            mt: 1.5,
                                            '& .MuiAvatar-root': {
                                                width: 32,
                                                height: 32,
                                                ml: -0.5,
                                                mr: 1
                                            },
                                            '&::before': {
                                                content: '""',
                                                display: 'block',
                                                position: 'absolute',
                                                top: 0,
                                                right: 14,
                                                width: 10,
                                                height: 10,
                                                bgcolor: 'background.paper',
                                                transform: 'translateY(-50%) rotate(45deg)',
                                                zIndex: 0
                                            }
                                        }
                                    }
                                }}
                                transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                                anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                            >
                                <MenuItem onClick={() => { handleCloseAccountMenu; nav(PAGES.ROOT + PAGES.PARENT_PROFILE) }}>
                                    <Avatar src={userInfo.imageUrl} alt={userInfo.fullName} /> Thông tin cá nhân
                                </MenuItem>
                                <Divider />
                                {
                                    userInfo.userType === "ApplicationUser" && (
                                        <MenuItem onClick={() => { handleCloseAccountMenu; nav(PAGES.ROOT + PAGES.CHANGE_PASSWORD) }}>
                                            <ListItemIcon>
                                                <Settings fontSize="small" />
                                            </ListItemIcon>
                                            Đổi mật khẩu
                                        </MenuItem>
                                    )
                                }
                                <MenuItem onClick={handleLogout}>
                                    <ListItemIcon>
                                        <Logout fontSize="small" />
                                    </ListItemIcon>
                                    Đăng xuất
                                </MenuItem>
                            </Menu>
                        </>
                    )
                }
                <NavigationMobile />
            </Stack>
        </Stack>
    )
}

export default Header
