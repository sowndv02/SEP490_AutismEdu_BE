import React, { useState } from 'react';
import { Box, Typography, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Pagination, IconButton, Button, FormControl, InputLabel, Select, MenuItem, TextField, InputAdornment } from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import SearchIcon from '@mui/icons-material/Search';
import { format } from 'date-fns';
import ConfirmAcceptDialog from './Modal/ConfirmAcceptDialog';
import ConfirmRejectDialog from './Modal/ConfirmRejectDialog';
import { enqueueSnackbar } from 'notistack';
import WorkExperienceDetail from './Modal/WorkExperienceDetail';
import emptyBook from '~/assets/images/icon/emptybook.gif'

const WorkExperienceManagement = () => {
    const [acceptDialogOpen, setAcceptDialogOpen] = useState(false);
    const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
    const [workExpDialogOpen, setWorkExpDialogOpen] = useState(false);
    const [currentRequestId, setCurrentRequestId] = useState(null);
    const [selectedWorkExp, setSelectedWorkExp] = useState(null);
    const [loading, setLoading] = useState(false);
    const [workExperiences, setWorkExperiences] = useState([]);
    const [filters, setFilters] = React.useState({
        search: '',
        status: 'all',
        orderBy: 'createdDate',
        sort: 'desc',
    });
    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 5,
        total: 5,
    });

    React.useEffect(() => {
        getWorkExperienceList();
    }, [filters, pagination.pageNumber]);


    const getWorkExperienceList = async () => {
        try {
            setLoading(true);
            await services.WorkExperiencesAPI.getWorkExperiences((res) => {
                if (res?.result) {
                    setWorkExperiences(res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                ...filters,
                pageNumber: pagination?.pageNumber
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleFilterChange = (key) => (event) => {
        setFilters({
            ...filters,
            [key]: event.target.value,
        });
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    const statusText = (status) => {
        switch (status) {
            case 0:
                return 'Từ chối';
            case 1:
                return 'Chấp nhận';
            case 2:
                return 'Chờ duyệt';
            default:
                return 'Không rõ';
        }
    };

    const handleChangeStatus = async (id, statusChange, rejectionReason) => {
        try {
            setLoading(true);
            await services.WorkExperiencesAPI.changeWorkExperienceStatus(id, {
                id,
                statusChange,
                rejectionReason
            }, (res) => {
                if (res?.result) {
                    let newData = workExperiences.find((r) => r.id === id);
                    newData.requestStatus = res.result.requestStatus;
                    newData.rejectionReason = res.result.rejectionReason;
                    setWorkExperiences(workExperiences);
                    enqueueSnackbar('Đổi trạng thái thành công!', { variant: 'success' });
                }
            }, (error) => {
                console.log(error);
            });

        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };


    const handleAccept = async (id) => {
        await handleChangeStatus(id, 1, "");
        console.log(`Accepted request ID: ${id}`);
    };

    const handleReject = async (id, reason) => {
        await handleChangeStatus(id, 0, reason);
        console.log(`Rejected request ID: ${id} with reason: ${reason}`);
    };

    return (
        <Box
            sx={{
                height: (theme) => `calc(90vh - ${theme.myapp.adminHeaderHeight})`,
                p: 0,
            }}
        >
            <Box
                sx={{
                    height: workExperiences?.length >= 5 ? 'auto' : '650px',
                    width: '100%',
                    bgcolor: 'white',
                    p: 3,
                    borderRadius: '10px',
                    boxShadow: 'rgba(0, 0, 0, 0.24) 0px 3px 8px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 2,
                }}
            >
                <Typography variant="h4" sx={{ mb: 3 }}>
                    Quản lý kinh nghiệm làm việc
                </Typography>

                <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
                    <Box sx={{ flex: 2, mr: 3 }}>
                        <TextField
                            disabled
                            fullWidth
                            size='small'
                            label="Tìm kiếm"
                            value={filters.search}
                            onChange={handleFilterChange('search')}
                            sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                            InputProps={{
                                endAdornment: (
                                    <InputAdornment position="end">
                                        <SearchIcon />
                                    </InputAdornment>
                                ),
                            }}
                        />
                    </Box>

                    <Stack direction={'row'} justifyContent={'flex-end'} spacing={2} sx={{ flex: 1 }}>
                        <Box sx={{ flexBasis: '55%' }}>
                            <FormControl fullWidth size='small'>
                                <InputLabel id="status-select-label">Trạng thái</InputLabel>
                                <Select
                                    labelId="status-select-label"
                                    value={filters.status}
                                    label="Trạng thái"
                                    onChange={handleFilterChange('status')}
                                    sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                >
                                    <MenuItem value={'all'}>Tất cả</MenuItem>
                                    <MenuItem value={'approve'}>Đã chấp nhận</MenuItem>
                                    <MenuItem value={'pending'}>Đang chờ</MenuItem>
                                    <MenuItem value={'reject'}>Từ chối</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>

                        <Box sx={{ flexBasis: '45%' }}>
                            <FormControl fullWidth size='small'>
                                <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                                <Select
                                    labelId="sort-select-label"
                                    value={filters.sort}
                                    label="Thứ tự"
                                    onChange={handleFilterChange('sort')}
                                    sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                >
                                    <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                                    <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>
                    </Stack>

                </Stack>

                {workExperiences.length === 0 ? (
                    <Box sx={{ textAlign: "center" }}>
                        <img src={emptyBook} style={{ height: "200px" }} />
                        <Typography>Hiện tại chưa có dữ liệu!</Typography>
                    </Box>
                ) : (
                    <>
                        <TableContainer component={Paper} sx={{ borderRadius: 2, boxShadow: 3 }}>
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell sx={{ fontWeight: 'bold' }}>ID</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Email</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Họ và tên</TableCell>
                                        {/* <TableCell sx={{ fontWeight: 'bold' }}>Tên công ty/doanh nghiệp</TableCell> */}
                                        {/* <TableCell sx={{ fontWeight: 'bold' }}>Vị trí</TableCell> */}
                                        <TableCell sx={{ fontWeight: 'bold' }}>Trạng thái</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {workExperiences.map((experience, index) => (
                                        <TableRow key={experience.id}>
                                            <TableCell>{index + 1 + (pagination.pageNumber - 1) * pagination.pageSize}</TableCell>
                                            <TableCell>{experience?.submitter?.email ?? experience?.tutorRegistrationRequest?.email}</TableCell>
                                            <TableCell>{experience?.submitter?.fullName ?? experience?.tutorRegistrationRequest?.fullName}</TableCell>
                                            {/* <TableCell>{experience.companyName}</TableCell> */}
                                            {/* <TableCell>{experience.position}</TableCell> */}
                                            <TableCell>
                                                <Button
                                                    variant="outlined"
                                                    color={
                                                        experience.requestStatus === 1 ? 'success' :
                                                            experience.requestStatus === 0 ? 'error' :
                                                                'warning'
                                                    }
                                                    size="small"
                                                    sx={{ borderRadius: 2, textTransform: 'none' }}
                                                >
                                                    {statusText(experience?.requestStatus)}
                                                </Button>
                                            </TableCell>
                                            <TableCell>{experience?.createdDate && format(new Date(experience?.createdDate), "HH:mm dd/MM/yyyy")}</TableCell>
                                            <TableCell>
                                                <Stack direction="row" spacing={1}>
                                                    <IconButton color="primary" onClick={() => { setSelectedWorkExp(experience); setWorkExpDialogOpen(true); }}>
                                                        <VisibilityIcon />
                                                    </IconButton>
                                                    {experience?.requestStatus === 2 && <>
                                                        <IconButton
                                                            color="success"
                                                            onClick={() => {
                                                                setCurrentRequestId(experience?.id);
                                                                setAcceptDialogOpen(true);
                                                            }}
                                                        >
                                                            <CheckCircleIcon />
                                                        </IconButton>
                                                        <IconButton
                                                            color="error"
                                                            onClick={() => {
                                                                setCurrentRequestId(experience?.id);
                                                                setRejectDialogOpen(true);
                                                            }}
                                                        >
                                                            <CancelIcon />
                                                        </IconButton>
                                                    </>}
                                                </Stack>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                            <Pagination
                                count={totalPages}
                                page={pagination.pageNumber}
                                onChange={handlePageChange}
                                color="primary"
                            />
                        </Stack>
                    </>
                )}
                <LoadingComponent open={loading} setOpen={setLoading} />

                {acceptDialogOpen && (
                    <ConfirmAcceptDialog
                        open={acceptDialogOpen}
                        onClose={() => setAcceptDialogOpen(false)}
                        onConfirm={() => {
                            handleAccept(currentRequestId);
                            setAcceptDialogOpen(false);
                        }}
                    />
                )}

                {rejectDialogOpen && (
                    <ConfirmRejectDialog
                        open={rejectDialogOpen}
                        onClose={() => setRejectDialogOpen(false)}
                        onConfirm={(reason) => {
                            handleReject(currentRequestId, reason);
                            setRejectDialogOpen(false);
                        }}
                    />
                )}
                {workExpDialogOpen && <WorkExperienceDetail open={workExpDialogOpen} onClose={() => setWorkExpDialogOpen(false)} workExperience={selectedWorkExp} />}
            </Box>
        </Box>
    );
};

export default WorkExperienceManagement;