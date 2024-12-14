import React, { useEffect, useState } from 'react';
import {
    Box,
    Typography,
    Avatar,
    Grid,
    Paper,
    Divider,
    Button,
    Skeleton,
} from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import LockIcon from '@mui/icons-material/Lock';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import { enqueueSnackbar } from 'notistack';
import { format } from 'date-fns';
import ConfirmDialog from '~/components/ConfirmDialog';

const ParentProfile = () => {
    const { id } = useParams();
    const [loading, setLoading] = useState(false);
    const [user, setUser] = useState(null);
    const nav = useNavigate();
    const [openDialog, setOpenDialog] = useState(false);
    const [openDialogg, setOpenDialogg] = useState(false);
    useEffect(() => {
        handleGerUserById();
    }, [id]);

    const handleGerUserById = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.getUserById(id, (res) => {
                if (res?.result) {
                    setUser(res.result);
                }
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const formatAddress = (address) => {
        return address ? address.split('|').reverse().join(', ') : 'Không có';
    };

    const handleLock = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.lockUsers(id, (res) => {
                if (res?.result) {
                    user.isLockedOut = res.result?.isLockedOut;
                    setUser(user);
                    enqueueSnackbar("Khoá tài khoản thành công!", { variant: 'success' });
                }
            }, (error) => {
                console.log(error);

            })
        } catch (error) {
            console.log(error);

        } finally {
            setOpenDialogg(false);
            setLoading(false);
        }
    };

    const handleUnlock = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.unLockUsers(id, (res) => {
                if (res?.result) {
                    user.isLockedOut = res.result?.isLockedOut;
                    setUser(user);
                    enqueueSnackbar("Mở khoá tài khoản thành công!", { variant: 'success' });
                }
            }, (error) => {
                console.log(error);

            })
        } catch (error) {
            console.log(error);

        } finally {
            setOpenDialog(false);
            setLoading(false);
        }
    };

    const renderSkeleton = () => (
        <Box sx={{ p: 3, maxWidth: '800px', mx: 'auto' }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Box sx={{ textAlign: 'center', mb: 3 }}>
                    <Skeleton variant="circular" width={120} height={120} sx={{ mx: 'auto', mb: 2 }} />
                    <Skeleton variant="text" width={200} height={32} sx={{ mx: 'auto', mb: 1 }} />
                    <Skeleton variant="text" width={150} height={20} sx={{ mx: 'auto' }} />
                </Box>

                <Divider sx={{ my: 3 }} />

                <Grid container spacing={2}>
                    {Array(5)
                        .fill(null)
                        .map((_, index) => (
                            <React.Fragment key={index}>
                                <Grid item xs={4}>
                                    <Skeleton variant="text" width="100%" height={24} />
                                </Grid>
                                <Grid item xs={8}>
                                    <Skeleton variant="text" width="80%" height={24} />
                                </Grid>
                            </React.Fragment>
                        ))}
                </Grid>
            </Paper>
        </Box>
    );


    return user ? (
        <Box sx={{ p: 3, maxWidth: '800px', mx: 'auto' }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Typography variant='h5' textAlign={'center'} mb={5}>Hồ sơ phụ huynh</Typography>
                <Box sx={{ textAlign: 'center', mb: 3 }}>
                    <Avatar
                        src={user?.imageUrl || ''}
                        alt={user?.fullName || ''}
                        sx={{
                            width: 120,
                            height: 120,
                            mx: 'auto',
                            mb: 2,
                            boxShadow: 3,
                        }}
                    />
                    <Typography variant="h5" fontWeight="bold">
                        {user.fullName}
                    </Typography>
                    <Typography variant="body1" color="text.secondary">
                        Vai trò: {user.role}
                    </Typography>
                </Box>

                <Divider sx={{ my: 3 }} />

                <Grid container spacing={2}>
                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: 'bold', color: 'text.secondary' }}>
                            Email:
                        </Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography>{user.email}</Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: 'bold', color: 'text.secondary' }}>
                            Số điện thoại:
                        </Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography>{user.phoneNumber}</Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: 'bold', color: 'text.secondary' }}>
                            Địa chỉ:
                        </Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography>{formatAddress(user.address)}</Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: 'bold', color: 'text.secondary' }}>
                            Ngày tạo:
                        </Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography>
                            {user?.createdDate ? format(new Date(user?.createdDate), 'dd/MM/yyyy') : 'N/A'}
                        </Typography>
                    </Grid>

                    <Grid item xs={4}>
                        <Typography sx={{ fontWeight: 'bold', color: 'text.secondary' }}>
                            Trạng thái:
                        </Typography>
                    </Grid>
                    <Grid item xs={8}>
                        <Typography
                            sx={{
                                display: 'inline-block',
                                px: 2,
                                py: 0.5,
                                backgroundColor: user.isLockedOut ? '#f8d7da' : '#d4edda',
                                color: user.isLockedOut ? 'red' : 'green',
                                borderRadius: '4px',
                                fontWeight: 'bold',
                            }}
                        >
                            {user.isLockedOut ? 'Khoá' : 'Hoạt động'}
                        </Typography>
                    </Grid>
                </Grid>

                <Divider sx={{ my: 3 }} />

                <Box display={'flex'} justifyContent={'center'} gap={2}>
                    <Button variant='outlined' color='inherit' onClick={() => nav(-1)}>
                        Quay lại
                    </Button>
                    {!user.isLockedOut ? <Button
                        startIcon={<LockIcon />}
                        variant="contained"
                        color="error"
                        onClick={() => setOpenDialogg(true)}
                    >
                        Khoá tài khoản
                    </Button>
                        :
                        <Button
                            startIcon={<LockOpenIcon />}
                            variant="contained"
                            color="success"
                            onClick={() => setOpenDialog(true)}
                        >
                            Mở khoá
                        </Button>}

                </Box>
            </Paper>
            {openDialog && <ConfirmDialog openConfirm={openDialog} setOpenConfirm={setOpenDialog}
                title={"Xác nhận"}
                content={"Bạn có muốn mở khoá người dùng này không?"}
                handleAction={handleUnlock}
            />}
            {openDialogg && <ConfirmDialog openConfirm={openDialogg} setOpenConfirm={setOpenDialogg}
                title={"Xác nhận"}
                content={"Bạn có muốn khoá người dùng này không?"}
                handleAction={handleLock}
            />}
            <LoadingComponent open={loading} setLoading={setLoading} />
        </Box>
    ) : (
        renderSkeleton()
    );
};

export default ParentProfile;