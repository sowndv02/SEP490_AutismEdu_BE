import AddIcon from '@mui/icons-material/Add';
import * as React from 'react';
import { Visibility as VisibilityIcon, Delete as DeleteIcon } from '@mui/icons-material';
import {
    Box,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    FormControl,
    IconButton,
    InputLabel,
    MenuItem,
    Pagination,
    Paper,
    Select,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Tooltip,
    Typography
} from '@mui/material';
import { useState } from 'react';
import { enqueueSnackbar } from 'notistack';
import services from '~/plugins/services';
import SearchIcon from '@mui/icons-material/Search';
import InputAdornment from '@mui/material/InputAdornment';
import LoadingComponent from '~/components/LoadingComponent';
import WorkExperienceCreation from './WorkExprerienceModal/WorkExperienceCreation';
import DeleteConfirmationModal from './WorkExprerienceModal/DeleteConfirmationModal';
import WorkExperienceDetail from './WorkExprerienceModal/WorkExperienceDetail';
import emptyBook from '~/assets/images/icon/emptybook.gif'
import { format } from 'date-fns';

function WorkExperienceManagement() {
    const [selectedContent, setSelectedContent] = useState('');
    const [selectedWorkExp, setSelectedWorkExp] = useState(null);
    const [workExpDialogOpen, setWorkExpDialogOpen] = useState(false);
    const [idDelete, setIdDelete] = useState(-1);
    const [loading, setLoading] = useState(false);
    const [openDialog, setOpenDialog] = useState(false);
    const [openDialogC, setOpenDialogC] = useState(false);
    const [filters, setFilters] = React.useState({
        search: '',
        status: 'all',
        orderBy: 'createdDate',
        sort: 'desc',
    });
    const [workExperienceData, setWorkExperienceData] = useState({
        "companyName": "",
        "position": "",
        "startDate": "",
        "endDate": "",
        "originalId": 0
    });

    const [workExperienceList, setWorkExperienceList] = useState([]);

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 5,
        total: 5,
    });

    const [open, setOpen] = useState(false);

    const handleClickOpen = (id) => {
        setIdDelete(id);
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };


    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleOpenDialog = (content) => {
        setSelectedContent(content);
        setOpenDialog(true);
    };

    const handleCloseDialog = () => {
        setOpenDialog(false);
        setSelectedContent('');
    };

    React.useEffect(() => {
        getWorkExperienceList();
    }, [filters, pagination.pageNumber]);

    const getWorkExperienceList = async () => {
        try {
            setLoading(true);
            await services.WorkExperiencesAPI.getWorkExperiences((res) => {
                if (res?.result) {
                    setWorkExperienceList(res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                search: filters.search,
                status: filters.status,
                orderBy: filters.orderBy,
                sort: filters.sort,
                pageNumber: pagination?.pageNumber
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
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

    const handleDialogOpen = () => {
        setOpenDialogC(true);
    };

    const handleDialogClose = () => {
        setOpenDialogC(false);
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setWorkExperienceData({ ...workExperienceData, [name]: value });
    };


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

    const formatDate = (dateString) => {
        return format(new Date(dateString), 'dd/MM/yyyy');
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    return (
        <Box sx={{ width: "90%", margin: "auto", mt: "20px", gap: 2 }}>
            <Typography variant='h4' sx={{ mb: 3 }}>Danh sách kinh nghiệm làm việc</Typography>
            <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
                <Box sx={{ flex: 2, mr: 3 }}>
                    <TextField
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
                    <Box sx={{ flexBasis: '45%' }}>
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

                <Button
                    variant="contained"
                    color="primary"
                    startIcon={<AddIcon />}
                    onClick={handleDialogOpen}
                    sx={{ height: '40px', whiteSpace: 'nowrap' }}
                >
                    Thêm kinh nghiệm làm việc
                </Button>
            </Stack>

            <Box>
                {workExperienceList.length === 0 ? <Box sx={{ textAlign: "center" }}>
                    <img src={emptyBook} style={{ height: "200px" }} />
                    <Typography>Hiện tại không có kinh nghiệm làm việc nào!</Typography>
                </Box> :
                    <TableContainer component={Paper} sx={{ mt: 3, boxShadow: 3, borderRadius: 2 }}>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Tên công ty/doanh nghiệp</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Vị trí</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Thời gian bắt đầu</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Thời gian kết thúc</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Trạng thái</TableCell>
                                    {/* <TableCell sx={{ fontWeight: 'bold' }}>Phản hồi</TableCell> */}
                                    <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {workExperienceList?.map((certificate, index) => (
                                    <TableRow key={certificate?.id} hover>
                                        <TableCell>{index + 1 + (pagination?.pageNumber - 1) * pagination?.pageSize}</TableCell>
                                        <TableCell>
                                            <Tooltip title={certificate?.companyName || ''} placement="top">
                                                <Box sx={{
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    whiteSpace: 'nowrap',
                                                    maxWidth: '100px',
                                                }} >
                                                    {certificate?.companyName}
                                                </Box>
                                            </Tooltip>
                                        </TableCell>
                                        <TableCell>{certificate?.position}</TableCell>
                                        <TableCell>{certificate?.startDate && formatDate(new Date(certificate.startDate), "dd/MM/yyyy")}</TableCell>
                                        <TableCell>{certificate?.endDate ? formatDate(new Date(certificate.endDate), "dd/MM/yyyy") : 'Hiện tại'}</TableCell>
                                        <TableCell>
                                            <Button
                                                variant="outlined"
                                                color={
                                                    certificate?.requestStatus === 1 ? 'success' :
                                                        certificate?.requestStatus === 0 ? 'error' :
                                                            'warning'
                                                }
                                                size="small"
                                                sx={{ borderRadius: 2, textTransform: 'none' }}
                                            >
                                                {statusText(certificate?.requestStatus)}
                                            </Button>
                                        </TableCell>
                                        {/* <TableCell>
                                            <Box sx={{ display: 'inline-flex', alignItems: 'center' }}>
                                                <Box sx={{
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    whiteSpace: 'nowrap',
                                                    maxWidth: 250
                                                }}>
                                                    {certificate?.rejectionReason || 'Chưa có phản hồi'}
                                                </Box>
                                                {certificate?.rejectionReason?.length > 35 && (
                                                    <Button
                                                        variant="text"
                                                        size="small"
                                                        onClick={() => handleOpenDialog(certificate?.rejectionReason)}
                                                        sx={{ textTransform: 'none', color: 'primary.main' }}
                                                    >
                                                        Xem thêm
                                                    </Button>
                                                )}
                                            </Box>
                                        </TableCell> */}
                                        <TableCell>
                                            <IconButton color="primary" onClick={() => { setSelectedWorkExp(certificate); setWorkExpDialogOpen(true); }}>
                                                <VisibilityIcon />
                                            </IconButton>
                                            {certificate?.requestStatus !== 2 && (<IconButton color="error" aria-label="xoá" onClick={() => handleClickOpen(certificate.id)}>
                                                <DeleteIcon />
                                            </IconButton>)}

                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>}

                <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
                    <DialogTitle textAlign={'center'}>Mô tả chi tiết</DialogTitle>
                    <Divider />
                    <DialogContent>
                        <Typography>{selectedContent}</Typography>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleCloseDialog} variant='outlined' color="primary">Đóng</Button>
                    </DialogActions>
                </Dialog>
                {workExperienceList.length !== 0 && <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                    <Pagination
                        count={totalPages}
                        page={pagination.pageNumber}
                        onChange={handlePageChange}
                        color="primary"
                    />
                </Stack>}
            </Box>

            <DeleteConfirmationModal open={open} handleClose={handleClose} id={idDelete} workExperienceList={workExperienceList} setWorkExperienceList={setWorkExperienceList} />

            <WorkExperienceCreation
                open={openDialogC}
                onClose={handleDialogClose}
                workExperienceList={workExperienceList}
                setWorkExperienceList={setWorkExperienceList}
            />

            <LoadingComponent open={loading} setOpen={setLoading} />
            {workExpDialogOpen && <WorkExperienceDetail open={workExpDialogOpen} onClose={() => setWorkExpDialogOpen(false)} workExperience={selectedWorkExp} />}
        </Box>
    );
}
export default WorkExperienceManagement;
